using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

class SplineOperationBuilder {
	private int size = -1;
	private List<Spline.Knot> knots = new List<Spline.Knot>();

	public bool Active => knots.Count > 0;

	public void AddValueArray(float[] values) {	
		if (this.size != -1) {
			throw new InvalidOperationException("already have a size");
		}

		if (values.Length != 5) {
			throw new InvalidOperationException("expected five elements");
		}
		if (values[2] != 0 || values[3] != 0 || values[4] != 0) {
			throw new InvalidOperationException("expected zero");
		}

		var knot = new Spline.Knot(values[0], values[1]);
		knots.Add(knot);
	}

	public void AddSize(int size) {
		if (this.size != -1) {
			throw new InvalidOperationException("already have a size");
		}

		if (size != knots.Count) {
			throw new InvalidOperationException("size doesn't match knot count");
		}

		this.size = size;
	}

	public void ConfirmInactive() {
		if (Active) {
			throw new InvalidOperationException("spline building in progress");
		}
	}

	public void Reset() {
		this.size = -1;
		this.knots.Clear();
	}

	public OperationRecipe Build() {
		if (this.size < 0) {
			throw new InvalidOperationException("missing size");
		}

		Spline spline = new Spline(knots.ToArray());
		OperationRecipe operation = OperationRecipe.MakeEvalSpline(spline);
		
		Reset();

		return operation;
	}
}

class FormulaImporter {
	private readonly DsonObjectLocator locator;
	private readonly string rootScope;

	private readonly List<FormulaRecipe> formulaRecipes = new List<FormulaRecipe>(); 

	public FormulaImporter(DsonObjectLocator locator, string rootScope) {
		this.locator = locator;
		this.rootScope = rootScope;
	}

	public IEnumerable<FormulaRecipe> FormulaRecipes => formulaRecipes;

	private string ConvertChannelUri(DsonTypes.DsonDocument doc, string uri) {
		string scope;
		int colonIdx = uri.IndexOf(':');
		if (colonIdx >= 0) {
			scope = Uri.UnescapeDataString(uri.Substring(0, colonIdx));
			uri = uri.Substring(colonIdx + 1);
		} else {
			scope = null;
		}

		string objectUri, query;

		int questionIdx = uri.IndexOf('?');
		if (questionIdx >= 0) {
			objectUri = uri.Substring(0, questionIdx);
			query = uri.Substring(questionIdx + 1);
		} else {
			objectUri = uri;
			query = null;
		}

		string resolvedObjectUri = doc.ResolveUri(objectUri);

		DsonTypes.DsonObject obj = doc.Locator.Locate(resolvedObjectUri);
		if (obj is DsonTypes.Modifier modifer) {
			if (modifer.channel.target_channel != null) {
				//Redirect alias channels to their target
				return ConvertChannelUri(doc, modifer.channel.target_channel);
			}
		}

		string objectName = obj.name;
		if (obj.id == scope) {
			//leave out the scope if it's redundant
			scope = null;
		}
		
		StringBuilder builder = new StringBuilder();
		if (scope != null && scope != rootScope) {
			builder.Append(scope).Append(":");
		}
		builder.Append(objectName);
		if (query != null) {
			builder.Append("?").Append(query);
		}
		return builder.ToString();
	}

	private void ImportOperation(SplineOperationBuilder splineOperationBuilder, List<OperationRecipe> operations, DsonTypes.DsonDocument doc, DsonTypes.Operation operation) {
		if (operation.op == DsonTypes.Operator.Push) {
			if (operation.url != null) {
				splineOperationBuilder.ConfirmInactive();
				string channelRef = ConvertChannelUri(doc, operation.url);
				operations.Add(OperationRecipe.MakePushChannel(channelRef));
			} else {
				Object val = operation.val;
				switch (val) {
					case JArray arrayValue:
						float[] values = arrayValue.ToObject<float[]>();
						splineOperationBuilder.AddValueArray(values);
						break;

					default:
						if (splineOperationBuilder.Active) {
							splineOperationBuilder.AddSize(Convert.ToInt32(operation.val));
						} else {
							float value = Convert.ToSingle(operation.val);
							operations.Add(OperationRecipe.MakePushConstant(value));
						}
						break;
				}
			}
		} else if (operation.op == DsonTypes.Operator.Add) {
			splineOperationBuilder.ConfirmInactive();
			operations.Add(OperationRecipe.Make(OperationRecipe.OperationKind.Add));
		} else if (operation.op == DsonTypes.Operator.Sub) {
			splineOperationBuilder.ConfirmInactive();
			operations.Add(OperationRecipe.Make(OperationRecipe.OperationKind.Sub));
		} else if (operation.op == DsonTypes.Operator.Mult) {
			splineOperationBuilder.ConfirmInactive();
			operations.Add(OperationRecipe.Make(OperationRecipe.OperationKind.Mul));
		} else if (operation.op == DsonTypes.Operator.Div) {
			splineOperationBuilder.ConfirmInactive();
			operations.Add(OperationRecipe.Make(OperationRecipe.OperationKind.Div));
		} else if (operation.op == DsonTypes.Operator.SplineTcb) {
			operations.Add(splineOperationBuilder.Build());
		}
	}

	public void ImportFormula(DsonTypes.DsonDocument doc, DsonTypes.Formula formula) {
		FormulaRecipe recipe = new FormulaRecipe {
			Output = ConvertChannelUri(doc, formula.output),
			Operations = new List<OperationRecipe>()
		};
		
		SplineOperationBuilder splineOperationBuilder = new SplineOperationBuilder();
		foreach (DsonTypes.Operation operation in formula.operations) {
			ImportOperation(splineOperationBuilder, recipe.Operations, doc, operation);
		}
		
		if (formula.stage == DsonTypes.Stage.Sum) {
			recipe.Stage = FormulaRecipe.FormulaStage.Sum;
		} else if (formula.stage == DsonTypes.Stage.Multiply) {
			recipe.Stage = FormulaRecipe.FormulaStage.Multiply;
		} else {
			throw new InvalidOperationException();
		}

		formulaRecipes.Add(recipe);
	}

	public void ImportFrom(DsonTypes.DsonDocument document) {
		if (document.Root.node_library != null) {
			foreach (var node in document.Root.node_library) {
				if (node.formulas != null) {
					foreach (var formula in node.formulas) {
						ImportFormula(document, formula);
					}
				}
			}
		}
		if (document.Root.modifier_library != null) {
			foreach (var modifier in document.Root.modifier_library) {
				if (modifier.formulas != null) {
					foreach (var formula in modifier.formulas) {
						ImportFormula(document, formula);
					}
				}
			}
		}
	}

	public static IEnumerable<FormulaRecipe> ImportForFigure(DsonObjectLocator locator, FigureUris figureUris) {
		FormulaImporter importer = new FormulaImporter(locator, figureUris.RootNodeId);

		importer.ImportFrom(locator.LocateRoot(figureUris.DocumentUri));
		foreach (DsonTypes.DsonDocument doc in locator.GetAllDocumentsUnderPath(figureUris.MorphsBasePath)) {
			importer.ImportFrom(doc);
		}

		return importer.FormulaRecipes;
	}
}

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

static class EvaluatorHelperMethods {
	public static double Clamp(double x, double min, double max) {
		if (x < min) {
			return min;
		} else if (x > max) {
			return max;
		} else {
			return x;
		}
	}

	public static double EvalSpline(double x, Spline spline) {
		return spline.Eval(x);
	}

	public static readonly MethodInfo ClampMethodInfo;
	public static readonly MethodInfo EvalSplineMethodInfo;

	static EvaluatorHelperMethods() {
		ClampMethodInfo = typeof(EvaluatorHelperMethods).GetMethod("Clamp", BindingFlags.Public | BindingFlags.Static);
		EvalSplineMethodInfo = typeof(EvaluatorHelperMethods).GetMethod("EvalSpline", BindingFlags.Public | BindingFlags.Static);
	}
}

public class DependencyGatheringVisitor : IOperationVisitor {
	public List<Channel> Dependencies { get; } = new List<Channel>();

	public void Add() {
	}

	public void Div() {
	}

	public void Mul() {
	}

	public void PushChannel(Channel channel) {
		Dependencies.Add(channel);
	}

	public void PushValue(double value) {
	}

	public void Spline(Spline spline) {
	}

	public void Sub() {
	}
}

class MethodGenerator : IOperationVisitor {
	public delegate void EvalDelegate(double[] parentValues, double[] rawValues, double[] valuesOut, Spline[] splines);

	private readonly List<Channel> channels;
	private HashSet<Channel> visitedChannels = new HashSet<Channel>();

	private readonly List<Spline> splines = new List<Spline>();

	private readonly DynamicMethod dynamicMethod;
	private readonly ILGenerator ilGenerator;
	private EvalDelegate evalDelegate;
	
	public MethodGenerator(List<Channel> channels) {
		this.channels = channels;

		this.dynamicMethod = new DynamicMethod(
			"EvalChannels",
			typeof(void),
			new [] {
				typeof(double[]), //parent values
				typeof(double[]), //raw values
				typeof(double[]), //valuesOut
				typeof(Spline[]) //splines
			},
			typeof(MethodGenerator));

		this.ilGenerator = dynamicMethod.GetILGenerator();
		ilGenerator.DeclareLocal(typeof(double));
	}

	public EvalDelegate Delegate => evalDelegate;
	public Spline[] Splines => splines.ToArray();
	
	private void GenerateFor(Channel channel) {
		if (visitedChannels.Contains(channel)) {
			return;
		}

		visitedChannels.Add(channel);
		
		//visit dependencies
		var dependencyGatheringVisitor = new DependencyGatheringVisitor();
		foreach (var formula in channel.SumFormulas) {
			formula.Accept(dependencyGatheringVisitor);
		}
		foreach (var formula in channel.MultiplyFormulas) {
			formula.Accept(dependencyGatheringVisitor);
		}
		foreach (var dependency in dependencyGatheringVisitor.Dependencies) {
			GenerateFor(dependency);
		}
				
		//setup stack for store
		ilGenerator.Emit(OpCodes.Ldarg_2);
		ilGenerator.Emit(OpCodes.Ldc_I4, channel.Index);

		//load raw value
		ilGenerator.Emit(OpCodes.Ldarg_1);
		ilGenerator.Emit(OpCodes.Ldc_I4, channel.Index);
		ilGenerator.Emit(OpCodes.Ldelem_R8);

		//add parent value
		if (channel.ParentChannel != null) {
			ilGenerator.Emit(OpCodes.Ldarg_0);
			ilGenerator.Emit(OpCodes.Ldc_I4, channel.ParentChannel.Index);
			ilGenerator.Emit(OpCodes.Ldelem_R8);
			ilGenerator.Emit(OpCodes.Add);
		}

		foreach (var formula in channel.SumFormulas) {
			formula.Accept(this);
			ilGenerator.Emit(OpCodes.Add);
		}

		foreach (var formula in channel.MultiplyFormulas) {
			formula.Accept(this);
			ilGenerator.Emit(OpCodes.Mul);
		}

		//apply clamp
		if (channel.Clamped) {
			ilGenerator.Emit(OpCodes.Ldc_R8, channel.Min);
			ilGenerator.Emit(OpCodes.Ldc_R8, channel.Max);
			ilGenerator.Emit(OpCodes.Call, EvaluatorHelperMethods.ClampMethodInfo);
		}
		
		//store result
		ilGenerator.Emit(OpCodes.Stelem_R8);
	}

	public void Generate() {
		foreach (var channel in channels) {
			GenerateFor(channel);
		}

		ilGenerator.Emit(OpCodes.Ret);

		this.evalDelegate = (EvalDelegate) dynamicMethod.CreateDelegate(typeof(EvalDelegate));
	}

	public void PushChannel(Channel channel) {
		ilGenerator.Emit(OpCodes.Ldarg_2);
		ilGenerator.Emit(OpCodes.Ldc_I4, channel.Index);
		ilGenerator.Emit(OpCodes.Ldelem_R8);
	}

	public void PushValue(double value) {
		ilGenerator.Emit(OpCodes.Ldc_R8, value);
	}

	public void Add() {
		ilGenerator.Emit(OpCodes.Add);
	}

	public void Mul() {
		ilGenerator.Emit(OpCodes.Mul);
	}

	public void Sub() {
		ilGenerator.Emit(OpCodes.Sub);
	}

	public void Div() {
		ilGenerator.Emit(OpCodes.Div);
	}

	public void Spline(Spline spline) {
		int splineIdx = splines.Count;
		splines.Add(spline);

		ilGenerator.Emit(OpCodes.Ldarg_3);
		ilGenerator.Emit(OpCodes.Ldc_I4, splineIdx);
		ilGenerator.Emit(OpCodes.Ldelem_Ref);
		ilGenerator.Emit(OpCodes.Call, EvaluatorHelperMethods.EvalSplineMethodInfo);
	}
}

public class ChannelEvaluator {
	private readonly int channelCount;
	private readonly MethodGenerator.EvalDelegate eval;
	private readonly Spline[] splines;

	public ChannelEvaluator(List<Channel> channels) {
		this.channelCount = channels.Count;

		var generator = new MethodGenerator(channels);
		generator.Generate();

		this.eval = generator.Delegate;
		this.splines = generator.Splines;
	}

	public ChannelOutputs Evaluate(ChannelOutputs parentOutputs, ChannelInputs inputs) {
		double[] valuesOut = new double[channelCount];
		
		eval(parentOutputs?.Values, inputs.RawValues, valuesOut, splines);

		return new ChannelOutputs(parentOutputs, valuesOut);
	}
}

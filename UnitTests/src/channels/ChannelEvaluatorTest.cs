using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

[TestClass]
public class ChannelEvaluatorTest {
	private const double Acc = 1e-4;
	private static readonly ChannelInputs EmptyInputs = new ChannelInputs(new double[] {});

	private void AssertFormulaEquals(double expectedResult, Formula formula) {
		Channel channel = new Channel("foo", 0, null, 0, 0, 0, false, false, null);
		channel.AttachSumFormula(formula);
		List<Channel> channels = new List<Channel> { channel };
		var evaluator = new ChannelEvaluator(channels);
		var inputs = new ChannelInputs(new double[] {0});
		var outputs = evaluator.Evaluate(null, inputs);
		Assert.AreEqual(expectedResult, outputs.Values[0], Acc);
	}

	[TestMethod]
	public void TestAdd() {
		var formula = new Formula(new IOperation[] {
			new PushValueOperation(2),
			new PushValueOperation(3),
			new AddOperation()
		});
		AssertFormulaEquals(5, formula);
	}

	[TestMethod]
	public void TestSub() {
		var formula = new Formula(new IOperation[] {
			new PushValueOperation(2),
			new PushValueOperation(3),
			new SubOperation()
		});
		AssertFormulaEquals(-1, formula);
	}

	[TestMethod]
	public void TestMul() {
		var formula = new Formula(new IOperation[] {
			new PushValueOperation(2),
			new PushValueOperation(3),
			new MulOperation()
		});
		AssertFormulaEquals(6, formula);
	}

	[TestMethod]
	public void TestDiv() {
		var formula = new Formula(new IOperation[] {
			new PushValueOperation(2),
			new PushValueOperation(3),
			new DivOperation()
		});
		AssertFormulaEquals(2/3d, formula);
	}

	[TestMethod]
	public void TestSpline() {
		Spline.Knot[] knots = new [] {
			new Spline.Knot(0f, 0f),
			new Spline.Knot(70f, 1f),
			new Spline.Knot(110f, 1f),
			new Spline.Knot(155.5f, 0f)
		};
		Spline spline = new Spline(knots);

		var formula = new Formula(new IOperation[] {
			new PushValueOperation(90),
			new SplineOperation(spline)
		});
		AssertFormulaEquals(1.1039, formula); //expected value from SplineTest
	}

	[TestMethod]
	public void TestRawValue() {
		Channel channel = new Channel("foo", 0, null, 0, 0, 0, false, false, null);
		List<Channel> channels = new List<Channel> { channel };
		var evaluator = new ChannelEvaluator(channels);
		var inputs = new ChannelInputs(new double[] { 42 });
		var outputs = evaluator.Evaluate(null, inputs);
		Assert.AreEqual(42, outputs.Values[0], Acc);
	}

	[TestMethod]
	public void TestPushChannel() {
		Channel channel0 = new Channel("foo", 0, null, 0, 0, 0, false, false, null);
		Channel channel1 = new Channel("bar", 1, null, 0, 0, 0, false, false, null);

		channel0.AttachSumFormula(new Formula(new IOperation[] {
			new PushChannelOperation(channel1)
		}));

		List<Channel> channels = new List<Channel> { channel0, channel1 };

		var evaluator = new ChannelEvaluator(channels);
		var inputs = new ChannelInputs(new double[] {0, 42});
		var outputs = evaluator.Evaluate(null, inputs);
		Assert.AreEqual(42, outputs.Values[0], Acc);
	}
}

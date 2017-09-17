using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class BinnerTest {
	private const float Acc = 1e-5f;

	[TestMethod]
	public void TestCount() {
		Binner indexer = new Binner(4, Binner.Mode.Midpoints);
		Assert.AreEqual(4, indexer.Count);
	}

	[TestMethod]
	public void TestMidpointsIdxToFloat() {
		Binner indexer = new Binner(4, Binner.Mode.Midpoints);
		Assert.AreEqual(1 / 8f, indexer.IdxToFloat(0), Acc);
		Assert.AreEqual(3 / 8f, indexer.IdxToFloat(1), Acc);
		Assert.AreEqual(5 / 8f, indexer.IdxToFloat(2), Acc);
		Assert.AreEqual(7 / 8f, indexer.IdxToFloat(3), Acc);
	}

	[TestMethod]
	public void TestMidpointsFloatToIdx() {
		Binner indexer = new Binner(4, Binner.Mode.Midpoints);

		//Test endpoints

		Assert.AreEqual(0, indexer.FloatToIdx(0));
		Assert.AreEqual(3, indexer.FloatToIdx(1));

		//Test ranges

		Assert.AreEqual(0, indexer.FloatToIdx(0 / 8f + Acc));
		Assert.AreEqual(0, indexer.FloatToIdx(1 / 8f));
		Assert.AreEqual(0, indexer.FloatToIdx(2 / 8f - Acc));

		Assert.AreEqual(1, indexer.FloatToIdx(2 / 8f + Acc));
		Assert.AreEqual(1, indexer.FloatToIdx(3 / 8f));
		Assert.AreEqual(1, indexer.FloatToIdx(4 / 8f - Acc));

		Assert.AreEqual(2, indexer.FloatToIdx(4 / 8f + Acc));
		Assert.AreEqual(2, indexer.FloatToIdx(5 / 8f));
		Assert.AreEqual(2, indexer.FloatToIdx(6 / 8f - Acc));

		Assert.AreEqual(3, indexer.FloatToIdx(6 / 8f + Acc));
		Assert.AreEqual(3, indexer.FloatToIdx(7 / 8f));
		Assert.AreEqual(3, indexer.FloatToIdx(8 / 8f - Acc));
		
		//Test beyond endpoints

		Assert.AreEqual(0, indexer.FloatToIdx(-1));
		Assert.AreEqual(3, indexer.FloatToIdx(2));
	}

	[TestMethod]
	public void TestEndpointsIdxToFloat() {
		Binner binner = new Binner(5, Binner.Mode.Endpoints);
		Assert.AreEqual(0, binner.IdxToFloat(0), Acc);
		Assert.AreEqual(0.25f, binner.IdxToFloat(1), Acc);
		Assert.AreEqual(0.5f, binner.IdxToFloat(2), Acc);
		Assert.AreEqual(0.75f, binner.IdxToFloat(3), Acc);
		Assert.AreEqual(1, binner.IdxToFloat(4), Acc);
	}

	[TestMethod]
	public void TestEndpointsFloatToIdx() {
		Binner binner = new Binner(5, Binner.Mode.Endpoints);

		//Test endpoints

		Assert.AreEqual(0, binner.FloatToIdx(0));
		Assert.AreEqual(4, binner.FloatToIdx(1));

		//Test ranges

		Assert.AreEqual(0, binner.FloatToIdx(0 / 8f + Acc));
		Assert.AreEqual(0, binner.FloatToIdx(1 / 8f - Acc));

		Assert.AreEqual(1, binner.FloatToIdx(1 / 8f + Acc));
		Assert.AreEqual(1, binner.FloatToIdx(2 / 8f));
		Assert.AreEqual(1, binner.FloatToIdx(3 / 8f - Acc));

		Assert.AreEqual(2, binner.FloatToIdx(3 / 8f + Acc));
		Assert.AreEqual(2, binner.FloatToIdx(4 / 8f));
		Assert.AreEqual(2, binner.FloatToIdx(5 / 8f - Acc));

		Assert.AreEqual(3, binner.FloatToIdx(5 / 8f + Acc));
		Assert.AreEqual(3, binner.FloatToIdx(6 / 8f));
		Assert.AreEqual(3, binner.FloatToIdx(7 / 8f - Acc));

		Assert.AreEqual(4, binner.FloatToIdx(7 / 8f + Acc));
		Assert.AreEqual(4, binner.FloatToIdx(8 / 8f - Acc));
		
		//Test beyond endpoints

		Assert.AreEqual(0, binner.FloatToIdx(-1));
		Assert.AreEqual(4, binner.FloatToIdx(2));
	}
}

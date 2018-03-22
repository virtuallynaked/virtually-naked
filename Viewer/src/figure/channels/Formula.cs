using System;

public class Formula {
	private readonly IOperation[] operations;

	public Formula(IOperation[] operations) {
		this.operations = operations;
	}

	public void Accept(IOperationVisitor visitor) {
		foreach (var operation in operations) {
			operation.Accept(visitor);
		}
	}
}

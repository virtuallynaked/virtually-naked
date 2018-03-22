public interface IOperation {
	void Accept(IOperationVisitor visitor);
}

public interface IOperationVisitor {
	void PushChannel(Channel channel);
	void PushValue(double value);
	void Add();
	void Mul();
	void Sub();
	void Div();
	void Spline(Spline spline);
}

public class PushChannelOperation : IOperation {
	private readonly Channel channel;

	public PushChannelOperation(Channel channel) {
		this.channel = channel;
	}

	public void Accept(IOperationVisitor visitor) {
		visitor.PushChannel(channel);
	}
}

public class PushValueOperation : IOperation {
	private readonly double value;

	public PushValueOperation(double value) {
		this.value = value;
	}

	public void Accept(IOperationVisitor visitor) {
		visitor.PushValue(value);
	}
}

public class AddOperation : IOperation {
	public void Accept(IOperationVisitor visitor) {
		visitor.Add();
	}
}

public class SubOperation : IOperation {
	public void Accept(IOperationVisitor visitor) {
		visitor.Sub();
	}
}

public class MulOperation : IOperation {
	public void Accept(IOperationVisitor visitor) {
		visitor.Mul();
	}
}

public class DivOperation : IOperation {
	public void Accept(IOperationVisitor visitor) {
		visitor.Div();
	}
}

public class SplineOperation : IOperation {
	private readonly Spline spline;

	public SplineOperation(Spline spline) {
		this.spline = spline;
	}

	public void Accept(IOperationVisitor visitor) {
		visitor.Spline(spline);
	}
}

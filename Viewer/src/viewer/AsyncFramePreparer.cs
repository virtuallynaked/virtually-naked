using System.Threading;

class AsyncFramePreparer {
	private readonly FramePreparer framePreparer;

	public AsyncFramePreparer(FramePreparer framePreparer) {
		this.framePreparer = framePreparer;

		var thread = new Thread(ThreadProc);
		thread.SetApartmentState(ApartmentState.STA);
		thread.IsBackground = true;
		thread.Start();
	}

	private readonly SemaphoreSlim updateParametersReadySemaphore = new SemaphoreSlim(0, 1);
	private FrameUpdateParameters updateParameters;
	private volatile IPreparedFrame preparedFrame;

	public void StartPreparingFrame(FrameUpdateParameters updateParameters) {
		this.updateParameters = updateParameters;
		this.preparedFrame = null;
		updateParametersReadySemaphore.Release();
	}

	private void ThreadProc() {
		while (true) {
			updateParametersReadySemaphore.Wait();
			preparedFrame = framePreparer.PrepareFrame(updateParameters);
		}
	}
	
	public IPreparedFrame FinishPreparingFrame() {
		SpinWait.SpinUntil(() => preparedFrame != null);
		return preparedFrame;
	}
}

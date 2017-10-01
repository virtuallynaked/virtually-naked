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

	private readonly SemaphoreSlim preparedFrameReadySemaphore = new SemaphoreSlim(0, 1);
	private IPreparedFrame preparedFrame;

	public void StartPreparingFrame(FrameUpdateParameters updateParameters) {
		this.updateParameters = updateParameters;
		updateParametersReadySemaphore.Release();
	}

	private void ThreadProc() {
		while (true) {
			updateParametersReadySemaphore.Wait();
			preparedFrame = framePreparer.PrepareFrame(updateParameters);
			preparedFrameReadySemaphore.Release();
		}
	}
	
	public IPreparedFrame FinishPreparingFrame() {
		preparedFrameReadySemaphore.Wait();
		return preparedFrame;
	}
}
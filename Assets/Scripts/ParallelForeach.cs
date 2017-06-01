using System;
using System.Threading;
using UnityEngine;
using Leap.Unity;

public class ParallelForeach {
  private Action<int, int> _action;

  private Worker[] _workers;
  private int _workersLeft;
  private object _finishedMonitor = new object();

  public Action OnComplete;

  public ParallelForeach(Action<int, int> action) : this(action, SystemInfo.processorCount) { }

  public ParallelForeach(Action<int, int> action, int threads) {
    _action = action;

    _workers = new Worker[threads];
    for (int i = 0; i < _workers.Length; i++) {
      _workers[i] = new Worker(this);
    }
  }

  public bool IsWorking {
    get {
      return _workersLeft != 0;
    }
  }

  public void Dispatch(int length) {
    if (IsWorking) {
      throw new InvalidOperationException("Cannot dispatch a parallel foreach while it is still in the middle of work.");
    }

    _workersLeft = _workers.Length;

    for (int i = 0; i < _workers.Length; i++) {
      Worker worker = _workers[i];
      worker.start = i * length / _workers.Length;
      worker.end = (i + 1) * length / _workers.Length;

      Monitor.Enter(worker.monitor);
      Monitor.Pulse(worker.monitor);
      Monitor.Exit(worker.monitor);
    }
  }

  public void Wait() {
    Monitor.Enter(_finishedMonitor);

    if (_workersLeft == 0) {
      Monitor.Exit(_finishedMonitor);
      return;
    }

    Monitor.Wait(_finishedMonitor);
    Monitor.Exit(_finishedMonitor);
  }

  private class Worker {
    public Thread thread;
    public int start;
    public int end;
    public object monitor = new object();

    private ParallelForeach _parent;

    public Worker(ParallelForeach parent) {
      _parent = parent;

      thread = new Thread(Run);
      thread.IsBackground = true;
      thread.Start();
    }

    public void Run() {
      Monitor.Enter(monitor);
      while (true) {
        Monitor.Wait(monitor);

        _parent._action(start, end);

        Monitor.Enter(_parent._finishedMonitor);
        int newValue = Interlocked.Decrement(ref _parent._workersLeft);
        if (newValue == 0) {
          if (_parent.OnComplete != null) {
            _parent.OnComplete();
          }
          Monitor.Pulse(_parent._finishedMonitor);
        }
        Monitor.Exit(_parent._finishedMonitor);
      }
    }
  }
}

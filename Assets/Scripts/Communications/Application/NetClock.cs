using System.Diagnostics;
using System.Threading;

public class NetClock
{
    private Stopwatch clock;
    private int offset;

    public NetClock()
    {
        if (clock == null)
        {
            clock = new Stopwatch();
            offset = 0;
        }
        else
        {
            Monitor.Enter(clock);
            clock.Stop();
            clock.Reset();
            Monitor.Exit(clock);
            offset = 0;
        }
    }

    public void Start()
    {
        Monitor.Enter(clock);
        clock.Start();
        Monitor.Exit(clock);
    }

    public void Stop()
    {
        Monitor.Enter(clock);
        clock.Stop();
        Monitor.Exit(clock);
    }

    public int GetTimeStamp()
    {
        int retVal;

        Monitor.Enter(clock);
        retVal = unchecked((int)clock.ElapsedMilliseconds) + offset; // I work with 'int' because it guarantees atomic operations (beware with race conditions)
        Monitor.Exit(clock);
        return retVal;
    }

    public void Restart()
    {
        Monitor.Enter(clock);
        clock.Restart();
        Monitor.Exit(clock);
    }

    public void Restart(int startingValue)
    {
        //UnityEngine.Debug.Log("Restarting...");
        Monitor.Enter(clock);
        offset = startingValue;
        clock.Restart();
        Monitor.Exit(clock);
    }
}

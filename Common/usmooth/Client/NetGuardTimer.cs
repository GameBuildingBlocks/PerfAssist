/*!lic_info

The MIT License (MIT)

Copyright (c) 2015 SeaSunOpenSource

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/


public class NetGuardTimer
{
    public const int TimeoutInMilliseconds = 3000;

    private System.Timers.Timer _timer;

    public event SysPost.StdMulticastDelegation Timeout;

    public void Activate()
    {
        _timer = new System.Timers.Timer(TimeoutInMilliseconds);
        _timer.Elapsed += OnTimeout;
        _timer.Start();
    }

    public void Deactivate()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer = null;
        }
    }
    void OnTimeout(object sender, System.Timers.ElapsedEventArgs e)
    {
        SysPost.InvokeMulticast(this, Timeout);
    }
}

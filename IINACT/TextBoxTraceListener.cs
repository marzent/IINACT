using System.Diagnostics;

namespace IINACT {
    internal class TextBoxTraceListener : TraceListener {
        private readonly TextBox _tBox;

        public TextBoxTraceListener(TextBox box) {
            _tBox = box;
        }

        public override void Write(string? msg) {
            if (msg == null) 
                return;
            new Thread(() => {
                _tBox.Parent.Invoke(new MethodInvoker(() => _tBox.AppendText(msg)));
            }).Start();
        }

        public override void WriteLine(string? msg) {
            Write(msg + "\r\n");
        }
    }
}

using System.Text;

namespace QLCMerge.Common
{
    public class FixedStringBuffer
    {
        public int MaxLength { get; }
        private StringBuilder _sb = new StringBuilder();

        public FixedStringBuffer(int maxLength)
        {
            MaxLength = maxLength;
        }

        public void Push(char c)
        {
            _sb.Insert(0, c);
            while (_sb.Length > MaxLength)
            {
                _sb.Remove(MaxLength, 1);
            }
        }

        public void Append(char c)
        {
            _sb.Append(c);
            while (_sb.Length > MaxLength)
            {
                _sb.Remove(0, 1);
            }
        }

        public void SafeAppend(char[] chars, int index, char? filler = null)
        {
            if (chars.Length > index)
            {
                this.Append(chars[index]);
            }
            else if (filler != null)
            {
                this.Append(filler.Value);
            }
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}

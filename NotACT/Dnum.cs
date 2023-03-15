namespace Advanced_Combat_Tracker
{
    public class Dnum : IComparable
    {
        private string damageString;

        public static Dnum NoDamage => 0L;

        public static Dnum Miss => -1L;

        public static Dnum Unknown => -9L;

        public static Dnum Death => -10L;

        public static Dnum ThreatPosition => -11L;

        public long Number { get; }

        public string DamageString
        {
            get => string.IsNullOrEmpty(damageString) ? ToString() : damageString;
            set => damageString = value;
        }

        public string DamageString2 { get; set; }

        public Dnum(long NumberValue)
        {
            Number = NumberValue;
            damageString = string.Empty;
        }

        public Dnum(long NumberValue, string CustomDamageString)
        {
            Number = NumberValue;
            damageString = CustomDamageString;
        }

        public static implicit operator long(Dnum val)
        {
            return val.Number;
        }

        public static implicit operator Dnum(long val) => val >= -10 ? new Dnum(val) : new Dnum(-9L);

        public static Dnum operator +(Dnum a, Dnum b)
        {
            if (a.Number > -1 && b.Number > -1)
            {
                return new Dnum(a.Number + b.Number);
            }

            if (a.Number < 0 && b.Number >= 0)
            {
                return new Dnum(b.Number);
            }

            if (b.Number < 0 && a.Number >= 0)
            {
                return new Dnum(a.Number);
            }

            return new Dnum(0L);
        }

        public static bool operator ==(Dnum a, Dnum b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Dnum a, Dnum b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            if (this.Number > 0 && string.IsNullOrEmpty(damageString))
            {
                return this.Number.ToString(ActGlobals.mainTableShowCommas ? "#,0" : "0");
            }

            var num = this.Number;
            var num2 = num - -10;
            if ((ulong)num2 > 10uL) return damageString + DamageString2;
            return num2 switch
            {
                10L => "data-dnumNoDamage",
                9L => "data-dnumMiss",
                8L => "data-dnumResist",
                7L => "data-dnumParry",
                6L => "data-dnumRiposte",
                5L => "data-dnumBlock",
                0L => "data-dnumDeath",
                _ => damageString + DamageString2
            };
        }

        public string ToString(bool ShortHand)
        {
            if (Number > 0)
            {
                return ShortHand ? Number.ToString() : Number.ToString(ActGlobals.mainTableShowCommas ? "#,0" : "0");
            }

            if (!ShortHand)
                return Number switch
                {
                    0L => "data-dnumNoDamage",
                    -1L => "data-dnumMiss",
                    -10L => "data-dnumDeath",
                    _ => damageString,
                };
            return Number switch
            {
                0L => "0",
                -1L => "data-dnumMiss",
                -10L => "data-dnumDeath",
                _ => damageString.Length < 3 ? damageString : damageString[..3]
            };
        }

        public int CompareTo(object? obj)
        {
            var dnum = (Dnum)obj!;
            var num = this.Number;
            var num2 = dnum.Number;
            if (num == -9 && num2 == -9)
            {
                return damageString.CompareTo(dnum.damageString);
            }

            return num.CompareTo(num2);
        }

        public override bool Equals(object? obj)
        {
            var dnum = (Dnum)obj!;
            if (dnum.Number != Number) return false;
            var text = DamageString;
            var value = dnum.DamageString;
            return text.Equals(value);
        }

        public override int GetHashCode()
        {
            return ToString(ShortHand: false).GetHashCode();
        }
    }
}

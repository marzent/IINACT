namespace Advanced_Combat_Tracker {
	public class StrDouble : IComparable, IEquatable<StrDouble> {
        public string Name { get; }

        public double Val { get; }

        public StrDouble(string Name, double Val) {
			this.Name = Name;
			this.Val = Val;
		}

		public int CompareTo(object? obj) {
			var obj2 = (StrDouble)obj!;
			var num = Val;
			var value = obj2.Val;
			return num.CompareTo(value);
		}

		public bool Equals(StrDouble? other) => Name == other!.Name && Val == other!.Val;
    }

}

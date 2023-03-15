namespace Advanced_Combat_Tracker
{
    public delegate void CombatActionDelegate(bool isImport, CombatActionEventArgs actionInfo);

    public class CombatActionEventArgs : EventArgs
    {
        public int swingType;

        public bool critical;

        public string attacker;

        public string theAttackType;

        public Dnum damage;

        public DateTime time;

        public int timeSorter;

        public string victim;

        public string theDamageType;

        public string special;

        public Dictionary<string, object> tags;

        public readonly MasterSwing combatAction;

        public bool cancelAction;

        public CombatActionEventArgs(MasterSwing CombatAction)
        {
            combatAction = CombatAction;
            swingType = CombatAction.SwingType;
            critical = CombatAction.Critical;
            attacker = CombatAction.Attacker;
            theAttackType = CombatAction.AttackType;
            damage = CombatAction.Damage;
            time = CombatAction.Time;
            timeSorter = CombatAction.TimeSorter;
            victim = CombatAction.Victim;
            theDamageType = CombatAction.DamageType;
            special = CombatAction.Special;
            tags = CombatAction.Tags;
        }

        [Obsolete]
        public CombatActionEventArgs(
            int SwingType, bool Critical, string Special, string Attacker, string TheAttackType, Dnum Damage,
            DateTime Time, int TimeSorter, string Victim, string TheDamageType)
        {
            swingType = SwingType;
            critical = Critical;
            attacker = Attacker;
            theAttackType = TheAttackType;
            damage = Damage;
            time = Time;
            timeSorter = TimeSorter;
            victim = Victim;
            theDamageType = TheDamageType;
            special = Special;
        }

        [Obsolete]
        public CombatActionEventArgs(
            int SwingType, bool Critical, string Attacker, string TheAttackType, Dnum Damage, DateTime Time,
            int TimeSorter, string Victim, string TheDamageType)
        {
            swingType = SwingType;
            critical = Critical;
            attacker = Attacker;
            theAttackType = TheAttackType;
            damage = Damage;
            time = Time;
            timeSorter = TimeSorter;
            victim = Victim;
            theDamageType = TheDamageType;
            special = "specialAttackTerm-none";
        }
    }
}

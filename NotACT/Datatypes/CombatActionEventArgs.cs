namespace Advanced_Combat_Tracker;

public delegate void CombatActionDelegate(bool isImport, CombatActionEventArgs actionInfo);

public class CombatActionEventArgs : EventArgs
{
    public readonly MasterSwing combatAction;

    public string attacker;

    public bool cancelAction;

    public bool critical;

    public Dnum damage;

    public string special;
    public int swingType;

    public Dictionary<string, object> tags;

    public string theAttackType;

    public string theDamageType;

    public DateTime time;

    public int timeSorter;

    public string victim;

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

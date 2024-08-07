﻿using Glamourer.Api.Enums;
public class Identifier {
    public long Id { get; set; }
    public bool IsItem { get; set; }
    public IdObject Item { get; set; }
    public Split Split { get; set; }
}

public class IdObject {
    public long Id { get; set; }
}
public class Level {
    public long Value { get; set; }
}

public class EquipObject {
    public EquipObject() {
        ItemId = new IdObject();
    }

    public string Name { get; set; }
    public Identifier Id { get; set; }
    public IdObject IconId { get; set; }
    public IdObject ModelId { get; set; }
    public WeaponType WeaponType { get; set; }
    //public Variant Variant { get; set; }
    public ApiEquipSlot Type { get; set; }
    public long Flags { get; set; }
    public Level Level { get; set; }
    public IdObject JobRestrictions { get; set; }
    public IdObject ItemId { get; set; }
    public bool Valid { get; set; }
    public string ModelString { get; set; }
}

public class Split {
    public IdObject Item1 { get; set; }
    public IdObject Item2 { get; set; }
    public IdObject Item3 { get; set; }
    public long Item4 { get; set; }
}

public class Variant {
    public long Id { get; set; }
}

public class WeaponType {
    public long Id { get; set; }
}
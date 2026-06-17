using System;

public interface ICustomComboManager
{
    ComboData RegisterServed(ComboInput input);
    ComboData GetComboData();
    Action<ComboData> OnMatch { get; set; }
}
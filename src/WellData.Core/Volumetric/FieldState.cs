namespace WellData.Core.Volumetric;

public enum FieldState
{
    /// <summary>Nothing was reported.</summary>
    Blank,

    /// <summary>Petrinex replaced the value with "***" for confidentiality.</summary>
    Masked,

    /// <summary>Something was reported but it could not be read as the expected type.</summary>
    Invalid,

    Valid,
}

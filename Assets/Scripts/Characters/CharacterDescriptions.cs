namespace TheTear.Characters
{
    public static class CharacterDescriptions
    {
        public static string GetDescription(CharacterMode mode)
        {
            switch (mode)
            {
                case CharacterMode.Matter:
                    return "MATTER: Sees what is physically present. Trusts surfaces and evidence as-is.";
                case CharacterMode.Void:
                    return "VOID: Sees what is missing. Absence outlines and hidden cavities appear.";
                case CharacterMode.Flow:
                    return "FLOW: Sees back in time. Movement traces and temporal echoes linger.";
                default:
                    return "";
            }
        }
    }
}

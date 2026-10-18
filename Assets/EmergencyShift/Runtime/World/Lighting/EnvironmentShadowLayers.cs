namespace EmergencyShift
{
    /// <summary>Rendering convention shared by authored environment and dynamically created remains.</summary>
    public static class EnvironmentShadowLayers
    {
        public const string Floor = "ES Floor";
        public const string Low = "ES Low";
        public const string Furniture = "ES Furniture";
        public const string Tall = "ES Tall";

        public static string Surface(EnvironmentHeightClass height)
        {
            switch (height)
            {
                case EnvironmentHeightClass.Low:
                    return Low;
                case EnvironmentHeightClass.Furniture:
                    return Furniture;
                case EnvironmentHeightClass.Tall:
                    return Tall;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(height));
            }
        }

        public static bool CanCastOnto(EnvironmentHeightClass caster, EnvironmentHeightClass receiver)
        {
            return caster >= receiver;
        }
    }
}

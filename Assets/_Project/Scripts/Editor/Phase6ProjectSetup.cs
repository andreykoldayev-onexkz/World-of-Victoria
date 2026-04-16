public static class Phase6ProjectSetup
{
    public static void Execute()
    {
        UrpPhase5ShaderSetup.Execute();
        UrpPhase6LightingSetup.Execute();
        UrpPhase11PolishSetup.Execute();
    }

    public static string Report()
    {
        return UrpPhase6LightingSetup.Report();
    }
}

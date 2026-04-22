namespace WorldOfVictoria.Core
{
    public interface ILevelListener
    {
        void OnTileChanged(int x, int y, int z);
        void OnLightColumnChanged(int x, int z, int oldDepth, int newDepth);
        void OnAllChanged();
    }
}

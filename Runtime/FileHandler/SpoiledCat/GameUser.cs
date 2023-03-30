sealed partial class GameUser
{
    private static GameUser instance;
    public static GameUser Instance => instance ??= Initialize();


    private GameUser()
    { }

    private static GameUser Initialize()
    {
        return InternalInitialize();
    }
}
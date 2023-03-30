#if UNITY_SWITCH && !UNITY_EDITOR
#define TT_UNITY_SWITCH
#endif

#if !TT_UNITY_SWITCH

sealed partial class GameUser
{
    private static GameUser InternalInitialize()
    {
        return new GameUser();
    }

}

#endif
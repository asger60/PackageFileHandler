#if UNITY_SWITCH && !UNITY_EDITOR
#define TT_UNITY_SWITCH
#endif

#if TT_UNITY_SWITCH
using nn.account;

sealed partial class GameUser
{
    private UserHandle user;
    private Uid uid;
    public Uid UserID => uid;

    private GameUser(ref UserHandle user) : this()
    {
        this.user = user;
        Account.GetLastOpenedUser(ref uid);
    }

    private static GameUser InternalInitialize()
    {
        Account.Initialize();
        UserHandle user = default;
        if (!Account.TryOpenPreselectedUser(ref user))
        {
            return null;
        }
        return new GameUser(ref user);
    }
}

#endif

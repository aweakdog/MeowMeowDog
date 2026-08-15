using Unity.Netcode.Components;

namespace MeowMeowDog.Net
{
    /// <summary>
    /// 客户端权威的位置同步：玩家自己移动自己，直接同步给其他人。
    /// 初版用这种方式手感最好（无输入延迟），后续如需防作弊可改服务器权威。
    /// </summary>
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}

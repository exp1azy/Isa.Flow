namespace Isa.Flow.Interact
{
    /// <summary>
    /// Константы.
    /// </summary>
    public static class Constant
    {
        /// <summary>
        /// Префикс имени очередей для работы RPC-запросов.
        /// </summary>
        public const string RpcQueueNamePrefix = "rpc_";

        /// <summary>
        /// Префикс имени обменника для работы широковещательных сообщений.
        /// </summary>
        public const string BroadcastExchangeNamePrefix = "broadcast_";

        /// <summary>
        /// Имя обменника для сообщений "я жив".
        /// </summary>
        public const string WhoAliveExchangeName = "who_alive";

        /// <summary>
        /// Промежуток времени между сигналами "я жив".
        /// </summary>
        public const int AliveSignalPeriol = 10000;
    }
}
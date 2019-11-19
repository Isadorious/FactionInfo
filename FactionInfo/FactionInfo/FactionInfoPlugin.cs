using NLog;
using Torch;
using Torch.API;

namespace FactionInfo
{
    public class FactionInfoPlugin : TorchPluginBase
    {
        public static readonly Logger Log = LogManager.GetLogger("FactionInfo");

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
        }
    }
}

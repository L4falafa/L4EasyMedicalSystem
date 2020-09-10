using Rocket.API;
using SDG.Unturned;

namespace Lafalafa.L4EasyMedicalSystem
{
    public class EasyMedicConfig : IRocketPluginConfiguration
    {

        public int DesfribilatorId { get; set; }
        public string ImageUrl { get; set; }

        public bool LockScreen { get; set; }

        public void LoadDefaults()
        {
            DesfribilatorId = 15;
            ImageUrl = "https://cdn.discordapp.com/attachments/661993286046711808/753380017903370311/latido-del-corazon.png";
            LockScreen = false;
        }

    }
}

namespace SmartRecorder
{
    internal class Program
    {

        static void Main(string[] args)
        {
            var deviceActions = new DeviceActions();
            deviceActions.SessionInit();
            int selection;
            do
            {
                selection = deviceActions.MainMenu();
            }
            while (selection != 0);
        }
    }
}

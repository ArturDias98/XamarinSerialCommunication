using Android.App;
using Android.Content;
using Android.Hardware.Usb;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Hoho.Android.UsbSerial.Driver;
using Hoho.Android.UsbSerial.Extensions;
using Hoho.Android.UsbSerial.Util;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: UsesFeature("android.hardware.usb.host")]
namespace UsbSerial
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    [IntentFilter(new[] { UsbManager.ActionUsbDeviceAttached })]
    [MetaData(UsbManager.ActionUsbDeviceAttached, Resource = "@xml/device_filter")]
    public class MainActivity : AppCompatActivity
    {
        UsbManager usbManager;
        UsbSerialPort selectedPort;
        List<UsbSerialPort> portList;
        SerialInputOutputManager serialIoManager;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            usbManager = GetSystemService(Context.UsbService) as UsbManager;
            
        }
        protected override async void OnResume()
        {
            base.OnResume();
            portList = new List<UsbSerialPort>();
            await PopulateListAsync();
            await Connect();
        }
        async Task Connect()
        {
            selectedPort = portList.FirstOrDefault();
            var permission = await usbManager.RequestPermissionAsync(selectedPort.Driver.Device, this);
            if (permission)
            {
                serialIoManager = new SerialInputOutputManager(selectedPort) 
                {
                    BaudRate = 115200,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                };

                serialIoManager.DataReceived += (sender, e) => {
                    RunOnUiThread(() => {
                        UpdateReceivedData(e.Data);
                    });
                };

                try
                {
                    serialIoManager.Open(usbManager);
                }
                catch (Java.IO.IOException e)
                {
                    //TODO:
                    return;
                }
            }
        }

        void UpdateReceivedData(byte[] data)
        {

        }
        async Task PopulateListAsync()
        {
            var drivers = await FindAllDriversAsync(usbManager);
            portList.Clear();
            foreach (var driver in drivers)
            {
                var ports = driver.Ports;
                foreach (var port in ports)
                {
                    portList.Add(port);
                }
            }
        }
        internal static Task<IList<IUsbSerialDriver>> FindAllDriversAsync(UsbManager usbManager)
        {            
            var table = UsbSerialProber.DefaultProbeTable;
            // adding a custom driver to the default probe table
            //table.AddProduct(0x1b4f, 0x0008, typeof(CdcAcmSerialDriver)); // IOIO OTG
            //table.AddProduct(0x09D8, 0x0420, typeof(CdcAcmSerialDriver)); // Elatec TWN4

            var prober = new UsbSerialProber(table);
            return prober.FindAllDriversAsync(usbManager);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
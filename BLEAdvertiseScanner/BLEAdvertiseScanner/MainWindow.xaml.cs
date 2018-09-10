using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;

namespace BLEAdvertiseScanner
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private BluetoothLEAdvertisementWatcher advWatcher;

        public MainWindow()
        {
            InitializeComponent();

            this.advWatcher = new BluetoothLEAdvertisementWatcher();

            // https://blogs.msdn.microsoft.com/shozoa/2016/02/28/windows-10-bluetooth/
            // インターバルがゼロのままだと、CPU負荷が高くなりますので、適切な間隔(SDK サンプルでは 1秒)に指定しないと、アプリの動作に支障をきたすことになります。
            this.advWatcher.SignalStrengthFilter.SamplingInterval = TimeSpan.FromMilliseconds(1000);

            // rssi >= -60のときスキャンする
            //this.advWatcher.SignalStrengthFilter.InRangeThresholdInDBm = -60;

            this.advWatcher.Received += this.Watcher_Received;

            // スキャン開始
            this.advWatcher.Start();

        }

        private async void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            await this.Dispatcher.InvokeAsync(() => {
                this.CheckArgs(args);
            });
        }

        public void CheckArgs(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // アドバタイズパケットBluetoothLEAdvertisementReceivedEventArgsの中身をチェック
            // コメントに記載されているP25などの記載はオライリー「Bluetooth Low Energyをはじめよう」の参考ページ番号

            Console.WriteLine("★アドバタイズパケットスキャン");

            // タイムスタンプ（スキャン日時？）
            DateTimeOffset timestamp = args.Timestamp;
            //bleDev.Memo = $"{args.Timestamp:HH\\:mm\\:ss}, RSSI: {args.RawSignalStrengthInDBm}, Address: {args.BluetoothAddress.ToString("X")}, Type: {args.AdvertisementType} , Name={args.Advertisement.LocalName}";
            Console.WriteLine($"TimeStamp = {timestamp:HH\\:mm\\:ss}");

            // アドバタイズパケット発信元の Bluetoothデバイスアドレス-48bit(6byte)(P21)
            // パブリックデバイスアドレスとランダムデバイスアドレスがあるが、どちらなのか？
            var bleAddress = args.BluetoothAddress;
            Console.WriteLine($"BluetoothAddress = {bleAddress.ToString("X")}");

            // シグナル強度
            var bleRSSI = args.RawSignalStrengthInDBm;
            Console.WriteLine($"RSSI = {bleRSSI}");

            // アドバタイズパケット種別(P25)
            // https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.advertisement.bluetoothleadvertisementtype
            BluetoothLEAdvertisementType advertisementType = args.AdvertisementType;
            Console.WriteLine($"AdvertisementType = {advertisementType}");
            switch (advertisementType) {
                case BluetoothLEAdvertisementType.ConnectableUndirected:
                    Console.WriteLine($"-> ADV_IND:コネクション可能、スキャン可能");
                    break;
                case BluetoothLEAdvertisementType.ConnectableDirected:
                    Console.WriteLine($"-> ADV_DIRECT_IND:コネクション可能、スキャン×");
                    break;
                case BluetoothLEAdvertisementType.ScannableUndirected:
                    Console.WriteLine($"-> ADV_SCAN_IND:コネクション×、スキャン〇");
                    break;
                case BluetoothLEAdvertisementType.NonConnectableUndirected:
                    Console.WriteLine($"-> ADV_NONCONN_IND:コネクション×、スキャン×");
                    break;
                case BluetoothLEAdvertisementType.ScanResponse:
                    Console.WriteLine($"-> SCAN_RSP:スキャン要求に対するスキャン応答");
                    break;
                default:
                    Console.WriteLine($"-> ???");
                    break;
            }

            // アドバタイズデータ（P54）
            Console.WriteLine("");
            Console.WriteLine("Data");

            // Flags　(DataSectionにも同じ情報あるが、こっちの方が見やすい）
            var bleFlags = args.Advertisement.Flags;
            Console.WriteLine($"Flags = {bleFlags}");

            // LocalName　(DataSectionにも同じ情報あるが、こっちの方が見やすい）
            Console.WriteLine($"LocalName = {args.Advertisement.LocalName}");

            // Service UUID　(DataSectionにも同じ情報あるが、こっちの方が見やすい）
            {
                var bleServiceUUIDs = args.Advertisement.ServiceUuids;
                Console.WriteLine($"Service UUID Num = {bleServiceUUIDs.Count}");
                foreach (var uuidone in bleServiceUUIDs) {
                    // サービスUUIDってなに？→http://jellyware.jp/kurage/bluejelly/uuid.html
                    Console.WriteLine($"-> Service UUID = {uuidone} -> {this.CheckServiceUUIDInfo(uuidone)}");
                }
                Console.WriteLine("");
            }

            {
                var manufacturerSections = args.Advertisement.ManufacturerData;
                Console.WriteLine($"Manufacturer Num = {manufacturerSections.Count}");
                if (manufacturerSections.Count > 0) {
                    foreach (var manuone in manufacturerSections) {
                        Console.WriteLine($"<Manufacturer>");
                        Console.WriteLine($"-> CompanyId = {manuone.CompanyId.ToString("X2")}");
                        Console.WriteLine($"-> Data Length = {manuone.Data.Length}");
                        if (manuone.Data.Length > 0) {
                            var data = new byte[manuone.Data.Length];
                            using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(manuone.Data)) {
                                reader.ReadBytes(data);

                                var tmp = BitConverter.ToString(data);
                                Console.WriteLine($"-> Data = {tmp}");
                            }
                        }
                    }
                }
                Console.WriteLine("");
            }


            // DataSection
            {
                var bleDataSections = args.Advertisement.DataSections;
                Console.WriteLine($"Advertising Data Num = {bleDataSections.Count}");
                foreach (var datasecone in bleDataSections) {
                    byte type = datasecone.DataType;
                    Console.WriteLine($"<Data>");
                    Console.WriteLine($"-> AD Type = 0x{type.ToString("X2")} -> {this.CheckADTypeInfo(type)}");
                    Console.WriteLine($"-> Data Length = {datasecone.Data.Length}");

                    if (datasecone.Data.Length > 0) {
                        var data = new byte[datasecone.Data.Length];
                        using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(datasecone.Data)) {
                            reader.ReadBytes(data);

                            var tmp = BitConverter.ToString(data);
                            Console.WriteLine($"-> Data = {tmp}");
                        }
                    }
                }
            }

            Console.WriteLine("");
            Console.WriteLine("");

        }

        public string CheckServiceUUIDInfo(Guid serviceuuid)
        {
            // 定義済みUUID→https://www.bluetooth.com/specifications/gatt/services
            var uuidtable = new Dictionary<string, string>() {
                                {"0x1800","Generic Access"},
                                {"0x1811","Alert Notification Service"},
                                {"0x1815","Automation IO"},
                                {"0x180F","Battery Service"},
                                {"0x1810","Blood Pressure"},
                                {"0x181B","Body Composition"},
                                {"0x181E","Bond Management Service"},
                                {"0x181F","Continuous Glucose Monitoring"},
                                {"0x1805","Current Time Service"},
                                {"0x1818","Cycling Power"},
                                {"0x1816","Cycling Speed and Cadence"},
                                {"0x180A","Device Information"},
                                {"0x181A","Environmental Sensing"},
                                {"0x1826","Fitness Machine"},
                                {"0x1801","Generic Attribute"},
                                {"0x1808","Glucose"},
                                {"0x1809","Health Thermometer"},
                                {"0x180D","Heart Rate"},
                                {"0x1823","HTTP Proxy"},
                                {"0x1812","Human Interface Device"},
                                {"0x1802","Immediate Alert"},
                                {"0x1821","Indoor Positioning"},
                                {"0x183A","Insulin Delivery"},
                                {"0x1820","Internet Protocol Support Service"},
                                {"0x1803","Link Loss"},
                                {"0x1819","Location and Navigation"},
                                {"0x1827","Mesh Provisioning Service"},
                                {"0x1828","Mesh Proxy Service"},
                                {"0x1807","Next DST Change Service"},
                                {"0x1825","Object Transfer Service"},
                                {"0x180E","Phone Alert Status Service"},
                                {"0x1822","Pulse Oximeter Service"},
                                {"0x1829","Reconnection Configuration"},
                                {"0x1806","Reference Time Update Service"},
                                {"0x1814","Running Speed and Cadence"},
                                {"0x1813","Scan Parameters"},
                                {"0x1824","Transport Discovery"},
                                {"0x1804","Tx Power"},
                                {"0x181C","User Data"},
                                {"0x181D","Weight Scale"},
                            };

            // Bluetooth SIGという団体にて標準で定義されているUUIDの部分、2Byte（4文字)を抽出 XXXXの部分
            // 0000XXXX-0000-1000-8000-00805f9b34fb
            string checkuuid = "0x" + serviceuuid.ToString().Substring(4, 4).ToUpper();
            string value = "";
            if (uuidtable.ContainsKey(checkuuid)) {
                value = uuidtable[checkuuid];
            } else {
                value = "Not Defined Service UUID";
            }

            return (value);
        }

        public string CheckADTypeInfo(byte adtype)
        {
            // データタイプ
            // https://sites.google.com/a/gclue.jp/ble-docs/advertising-1/advertising#TOC-Ad-Type
            // https://www.bluetooth.com/ja-jp/specifications/assigned-numbers/generic-access-profile
            var table = new Dictionary<string, string>() {
                                {"0x01","«Flags»"},
                                {"0x02","«Incomplete List of 16-bit Service Class UUIDs»"},
                                {"0x03","«Complete List of 16-bit Service Class UUIDs»"},
                                {"0x04","«Incomplete List of 32-bit Service Class UUIDs»"},
                                {"0x05","«Complete List of 32-bit Service Class UUIDs»"},
                                {"0x06","«Incomplete List of 128-bit Service Class UUIDs»"},
                                {"0x07","«Complete List of 128-bit Service Class UUIDs»"},
                                {"0x08","«Shortened Local Name»"},
                                {"0x09","«Complete Local Name»"},
                                {"0x0A","«Tx Power Level»"},
                                {"0x0D","«Class of Device»"},
//                                {"0x0E","«Simple Pairing Hash C»"},
                                {"0x0E","«Simple Pairing Hash C-192»"},
//                                {"0x0F","«Simple Pairing Randomizer R»"},
                                {"0x0F","«Simple Pairing Randomizer R-192»"},
                                {"0x10","«Device ID»"},
//                                {"0x10","«Security Manager TK Value»"},
                                {"0x11","«Security Manager Out of Band Flags»"},
                                {"0x12","«Slave Connection Interval Range»"},
                                {"0x14","«List of 16-bit Service Solicitation UUIDs»"},
                                {"0x15","«List of 128-bit Service Solicitation UUIDs»"},
//                                {"0x16","«Service Data»"},
                                {"0x16","«Service Data - 16-bit UUID»"},
                                {"0x17","«Public Target Address»"},
                                {"0x18","«Random Target Address»"},
                                {"0x19","«Appearance»"},
                                {"0x1A","«Advertising Interval»"},
                                {"0x1B","«LE Bluetooth Device Address»"},
                                {"0x1C","«LE Role»"},
                                {"0x1D","«Simple Pairing Hash C-256»"},
                                {"0x1E","«Simple Pairing Randomizer R-256»"},
                                {"0x1F","«List of 32-bit Service Solicitation UUIDs»"},
                                {"0x20","«Service Data - 32-bit UUID»"},
                                {"0x21","«Service Data - 128-bit UUID»"},
                                {"0x22","«LE Secure Connections Confirmation Value»"},
                                {"0x23","«LE Secure Connections Random Value»"},
                                {"0x24","«URI»"},
                                {"0x25","«Indoor Positioning»"},
                                {"0x26","«Transport Discovery Data»"},
                                {"0x27","«LE Supported Features»"},
                                {"0x28","«Channel Map Update Indication»"},
                                {"0x29","«PB-ADV»Mesh Profile Specification Section 5.2.1"},
                                {"0x2A","«Mesh Message»Mesh Profile Specification Section 3.3.1"},
                                {"0x2B","«Mesh Beacon»Mesh Profile Specification Section 3.9"},
                                {"0x3D","«3D Information Data»"},
                                {"0xFF","«Manufacturer Specific Data»"},
                            };

            string value = "";
            string searchkey = "0x" + adtype.ToString("X2");

            if (table.ContainsKey(searchkey)) {
                value = table[searchkey];
            } else {
                value = "Not Defined AD Type";
            }

            return (value);
        }

    }
}

package com.example.gego.bleadvertisescannerforandroid;

import android.Manifest;
import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothManager;
import android.bluetooth.le.BluetoothLeScanner;
import android.bluetooth.le.ScanCallback;
import android.bluetooth.le.ScanFilter;
import android.bluetooth.le.ScanResult;
import android.bluetooth.le.ScanSettings;
import android.content.Context;
import android.content.pm.PackageManager;
import android.os.ParcelUuid;
import android.os.SystemClock;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;
import android.util.SparseArray;
import android.view.View;
import android.widget.Button;
import android.widget.TextView;

import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class MainActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        Button ready_button = (Button)findViewById(R.id.ready_button);
        ready_button.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                onClickReady(v);
            }
        });

    }

    private void onClickReady(View v) {
        BluetoothManager manager = (BluetoothManager) getSystemService(Context.BLUETOOTH_SERVICE);
        BluetoothAdapter adapter = manager.getAdapter();
        final BluetoothLeScanner bluetoothLeScanner = adapter.getBluetoothLeScanner();

        // 6.0以降はコメントアウトした処理をしないと初回はパーミッションがOFFになっています。
        if (checkSelfPermission(Manifest.permission.ACCESS_COARSE_LOCATION) != PackageManager.PERMISSION_GRANTED) {
            requestPermissions(new String[]{Manifest.permission.ACCESS_COARSE_LOCATION}, 0);
        }

        final ScanCallback scanCallback = new ScanCallback() {
            @Override
            public void onScanResult(int callbackType, ScanResult result) {
                super.onScanResult(callbackType, result);
                onScanResultMethod(callbackType,result);
            }

            @Override
            public void onBatchScanResults(List<ScanResult> results) {
                super.onBatchScanResults(results);
            }

            @Override
            public void onScanFailed(int errorCode) {
                super.onScanFailed(errorCode);
            }
        };

        findViewById(R.id.start_button).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                ((TextView) findViewById(R.id.text1)).setText("Start!");

                ScanFilter scanFilter =
                        new ScanFilter.Builder()
                                //.setDeviceName("Health")
                                .build();
                ArrayList scanFilterList = new ArrayList();
                scanFilterList.add(scanFilter);

                ScanSettings scanSettings = new ScanSettings.Builder().setScanMode(ScanSettings.SCAN_MODE_BALANCED).build();

                bluetoothLeScanner.startScan(scanFilterList, scanSettings, scanCallback);

                ((TextView) findViewById(R.id.text1)).setText("Started!");
            }
        });

        findViewById(R.id.stop_button).setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                bluetoothLeScanner.stopScan(scanCallback);
            }
        });

        ((TextView) findViewById(R.id.text1)).setText("Ready!");

    }

    private void onScanResultMethod(int callbackType, ScanResult result) {
        Log.d("","★アドバタイズパケットスキャン");
        Log.d("", "callbackType = " + callbackType);
        Log.d("", "TimeStamp = " + convNnosToDatetime(result.getTimestampNanos()));
        Log.d("", "BluetoothAddress = " + result.getDevice().getAddress());
        Log.d("", "RSSI = " + String.format(("%d"),result.getRssi()));

        // requires API lebel 26
        //Log.d("", "isConnectable = " + String.format(("%d"),result.isConnectable()));
        //Log.d("", "TxPower = " + String.format(("%d"),result.getTxPower()));
        //Log.d("", "AdvertisingSid = " + String.format("%d",result.getAdvertisingSid()));

        Log.d("","");
        Log.d("","Data");
        Log.d("", "Name = " + result.getDevice().getName());
        List<ParcelUuid> serviceUuids = result.getScanRecord().getServiceUuids();
        if( serviceUuids != null ) {
            Log.d("", "Service UUID Num = " + serviceUuids.size());
            for (ParcelUuid uuid : serviceUuids) {
                Log.d("", "-> Service UUID = " + uuid.toString());
            }
        }

        int advertiseFlags = result.getScanRecord().getAdvertiseFlags();
        SparseArray<byte[]> manufacturerSpecificData = result.getScanRecord().getManufacturerSpecificData();

        for(int i = 0, nsize = manufacturerSpecificData.size(); i < nsize; i++) {
            byte[] obj = manufacturerSpecificData.valueAt(i);
            Log.d("", String.format("manufacturerSpecificData = %s(%s)",convByteToHexString(obj),convByteToAsciiString(obj)));
        }

        // アドバタイズデータを解析
        parseAdvertisingData(result.getScanRecord().getBytes());
    }

    public void parseAdvertisingData(byte[] data){
        if( data == null ){
            Log.d("", "AdvertisingData = null");
            return;
        }

        // Raw
        Log.d("", "AdvertisingData Raw = " + convByteToHexString(data));

        // Data Length(2byte)
        // AD Type(1byte)
        // Data(xbyte)
        // の繰り返し
        {
            for ( int intIc = 0 ; intIc < data.length ;){
                if(data[intIc] == 0 ){
                    break;
                }
                Log.d("", "<Data>");

                byte data_length = data[intIc];
                Log.d("", String.format("-> Data Length = %d", data_length) );

                byte ad_type = data[intIc+1];
                Log.d("", String.format("-> AD Type = 0x%02x -> %s ", ad_type,checkADTypeInfo(ad_type)) );

                byte[] ad_data = new byte[data_length-1];
                System.arraycopy(data,intIc+2,ad_data,0,ad_data.length); //data[intIc+2] → ad_dataにコピー
                Log.d("", String.format("-> Data = %s(%s)", convByteToHexString(ad_data),convByteToAsciiString(ad_data)) );

                intIc = intIc+1+data_length;
            }
        }

    }
    public static String convByteToHexString(byte[] data) {
        String ret = "";
        for (byte b : data) {
            ret = ret + String.format("%02x-", b);
        }
        if (ret.length() > 0) {
            ret = ret.substring(0, ret.length() - 1);
        }
        return(ret);
    }
    public static String convByteToAsciiString(byte[] data) {
        String ret = new String(data);
        return(ret);
    }

    public static String convNnosToDatetime(long timestampnanos){
        long rxTimestampMillis = System.currentTimeMillis() -
                SystemClock.elapsedRealtime() +
                timestampnanos / 1000000;

        Date rxDate = new Date(rxTimestampMillis);

        String sDate = new SimpleDateFormat("yyyy/MM/dd HH:mm:ss.SSS").format(rxDate);

        return sDate;
    }

    public static String checkADTypeInfo(byte adtype){
        // データタイプ
        // https://sites.google.com/a/gclue.jp/ble-docs/advertising-1/advertising#TOC-Ad-Type
        // https://www.bluetooth.com/ja-jp/specifications/assigned-numbers/generic-access-profile
        Map<String, String> table = new HashMap<String, String>() {
            {
                put("0x01","«Flags»");
                put("0x02","«Incomplete List of 16-bit Service Class UUIDs»");
                put("0x03","«Complete List of 16-bit Service Class UUIDs»");
                put("0x04","«Incomplete List of 32-bit Service Class UUIDs»");
                put("0x05","«Complete List of 32-bit Service Class UUIDs»");
                put("0x06","«Incomplete List of 128-bit Service Class UUIDs»");
                put("0x07","«Complete List of 128-bit Service Class UUIDs»");
                put("0x08","«Shortened Local Name»");
                put("0x09","«Complete Local Name»");
                put("0x0A","«Tx Power Level»");
                put("0x0D","«Class of Device»");
                //{"0x0E","«Simple Pairing Hash C»"},
                put("0x0E","«Simple Pairing Hash C-192»");
                //{"0x0F","«Simple Pairing Randomizer R»"},
                put("0x0F","«Simple Pairing Randomizer R-192»");
                put("0x10","«Device ID»");
                //{"0x10","«Security Manager TK Value»"},
                put("0x11","«Security Manager Out of Band Flags»");
                put("0x12","«Slave Connection Interval Range»");
                put("0x14","«List of 16-bit Service Solicitation UUIDs»");
                put("0x15","«List of 128-bit Service Solicitation UUIDs»");
                //{"0x16","«Service Data»"},
                put("0x16","«Service Data - 16-bit UUID»");
                put("0x17","«Public Target Address»");
                put("0x18","«Random Target Address»");
                put("0x19","«Appearance»");
                put("0x1A","«Advertising Interval»");
                put("0x1B","«LE Bluetooth Device Address»");
                put("0x1C","«LE Role»");
                put("0x1D","«Simple Pairing Hash C-256»");
                put("0x1E","«Simple Pairing Randomizer R-256»");
                put("0x1F","«List of 32-bit Service Solicitation UUIDs»");
                put("0x20","«Service Data - 32-bit UUID»");
                put("0x21","«Service Data - 128-bit UUID»");
                put("0x22","«LE Secure Connections Confirmation Value»");
                put("0x23","«LE Secure Connections Random Value»");
                put("0x24","«URI»");
                put("0x25","«Indoor Positioning»");
                put("0x26","«Transport Discovery Data»");
                put("0x27","«LE Supported Features»");
                put("0x28","«Channel Map Update Indication»");
                put("0x29","«PB-ADV»Mesh Profile Specification Section 5.2.1");
                put("0x2A","«Mesh Message»Mesh Profile Specification Section 3.3.1");
                put("0x2B","«Mesh Beacon»Mesh Profile Specification Section 3.9");
                put("0x3D","«3D Information Data»");
                put("0xFF","«Manufacturer Specific Data»");
            }
        };

        String value = "";
        String searchkey = "0x" + String.format("%02x",adtype).toUpperCase();

        if (table.containsKey(searchkey)) {
            value = table.get(searchkey);
        } else {
            value = "Not Defined AD Type";
        }

        return (value);
    }

}

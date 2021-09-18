package com.xvisio.unity;
import com.unity3d.player.UnityPlayerActivity;

import android.Manifest;
import android.os.Bundle;
import android.util.Log;
import android.content.Context;
import android.content.pm.PackageManager;

import android.content.Context;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;
import android.util.Log;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.Iterator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

import org.xvisio.devicewatcher.DeviceListener;
import org.xvisio.devicewatcher.DeviceWatcher;

public class XVisioSDKDemo extends UnityPlayerActivity {

    private static final String TAG = "xvsdk-demo";

    private static final int PERMISSIONS_REQUEST_CAMERA = 0;
    private boolean mPermissionsGranted = false;

    private Context mAppContext;
    //static private XCamera mCamera;

	static public double posX = 0;
	static public double posY = 0;
	static public double posZ = 0;
	static public double posRoll = 0;
	static public double posPitch = 0;
	static public double posYaw = 0;

	static public int m_fd = -1;

	static private Object mutex = new Object();
	
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        mAppContext = getApplicationContext();

        if (android.os.Build.VERSION.SDK_INT > android.os.Build.VERSION_CODES.O &&
                checkSelfPermission( Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {
            requestPermissions( new String[]{Manifest.permission.CAMERA}, PERMISSIONS_REQUEST_CAMERA);
            return;
        }

        mPermissionsGranted = true;
        
        init();
    }


    @Override
    public void onRequestPermissionsResult(int requestCode, String permissions[], int[] grantResults) {
        if (checkSelfPermission( Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {
            requestPermissions( new String[]{Manifest.permission.CAMERA}, PERMISSIONS_REQUEST_CAMERA);
            return;
        }
        mPermissionsGranted = true;
        
        init();
    }

	@Override
    protected void onResume() {
        super.onResume();
        if(mPermissionsGranted){
            init();
        }else{
            Log.e(TAG, "missing permissions");
		}
    }

    @Override
    protected void onPause() {
        super.onPause();
        /*if(mCamera != null){
            mCamera.pause();
		}*/
    }

    private DeviceListener mListener = new DeviceListener() {
        @Override
        public void onDeviceAttach() {
        	Log.d(TAG, "onDeviceAttach");
        	
        	UsbManager usbManager = (UsbManager) mAppContext.getSystemService(Context.USB_SERVICE);
		    HashMap<String, UsbDevice> devicesMap = usbManager.getDeviceList();
		    List<String> xvisioDevices = new ArrayList<String>();
		    
		    UsbDevice device = null;
		    
		    for (Map.Entry<String, UsbDevice> entry : devicesMap.entrySet()) {
		        UsbDevice usbDevice = entry.getValue();
		        if (isXVisio(usbDevice)){
		            //xvisioDevices.add(entry.getKey());
		            device = usbDevice;
		        }
		    }
		    
		    if(device == null){
		    	Log.e(TAG, "Failed to find device");
		        return;
		    }
		    UsbDeviceConnection conn = usbManager.openDevice(device);
		    if(conn == null){
		    	Log.e(TAG, "Failed to open device");
		        return;
		    }
		    String name = device.getDeviceName();
		    int fd = conn.getFileDescriptor();   
		    	
			Log.w(TAG, "Fd = " + fd);  
			m_fd = fd; 
        }

        @Override
        public void onDeviceDetach() {
        	Log.d(TAG, "onDeviceDetach");
        	m_fd = -1;
        }
    };
    
    
    
    
	public static boolean isXVisio(UsbDevice usbDevice){
        if (usbDevice.getVendorId() == 0x040e)
            return true;
        return false;
    }
    
    private static DeviceWatcher mDeviceWatcher = null;    
    private void init(){   
    	if(  mDeviceWatcher == null ){
        	mDeviceWatcher = new DeviceWatcher(mAppContext);
        	mDeviceWatcher.addListener(mListener);
        }
    }
    
    public static int getFd() {
    	return m_fd;
    }
}


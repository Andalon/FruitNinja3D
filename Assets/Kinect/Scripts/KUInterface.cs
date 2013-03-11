/************************************************************************
*                                                                       *
*				      KinectSDK-Unity3D C# Wrapper:                     *
*	Attach to a GameObject and ensure that KUInterfaceCPP.dll is in     *
*	                  the game's working directory.                     *
*																		*
*						  Author: Andrew DeVine							*
*				  University of Central Florida ISUE Lab				*
*								  2011									*
*		   (see included BSD license for licensing information)         *
************************************************************************/

using UnityEngine;
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;
//using Microsoft.Kinect;
//using Microsoft.Speech.AudioFormat;
//using Microsoft.Speech.Recognition;

/// <summary>
/// class for accessing Kinect methods
/// </summary>
public class KUInterface : MonoBehaviour {

    //public variables
    /// <summary>
    /// scales all joint positions by given amount. Do not set to zero.
    /// </summary>
    public int scaleFactor = 1000;
    /// <summary>
    /// set to true to track two skeletons
    /// </summary>
    public bool twoPlayer = false;
    /// <summary>
    /// set to false to optimize performance if RGB camera is not being used
    /// </summary>
    public bool useRGB = true;
    /// <summary>
    /// set to false to optimize performance if depth camera is not being used
    /// </summary>
    public bool useDepth = true;
    /// <summary>
    /// displays joint position data on screen
    /// </summary>
    public bool displayJointInformation = false;
    /// <summary>
    /// displays RGB texture image on screen
    /// </summary>
    public bool displayTextureImage = false;
    /// <summary>
    /// displays depth image on screen
    /// </summary>
    public bool displayDepthImage = false;

    //constants
    private const int IM_W = 640;
    private const int IM_H = 480;

    //private variables
    private bool NUIisReady = false;
    private float lastCameraAngleChange = -30.0f;
    private byte[] seqTex;
    private Color32[] cols;
    private Texture2D texture;
    private byte[] seqDepth;
    private Color32[] dcols;
    private Texture2D depthImg;
    private byte[][] depth;
	
	// Project Variables
	private bool started = false;
	private bool savedData = false;
	private CharacterMotor motor;
	private Vector3 lastRightFoot;
	private Vector3 lastLeftFoot;
	private Vector3 baseFeetHeight;
	private Vector3 baseHeadHeight;
	
	//Speech variables
	private bool audioIsReady;
	public bool displayAudioInformation = false;
	
/********************************************************************************
*           USER METHODS -> Call these methods from your scripts
* ******************************************************************************/

    /// <summary>
    /// main joint position get function
    /// </summary>
    /// <param name="player">player number (1,2)</param>
    /// <param name="joint">KinectWrapper.Joints enum</param>
    /// <returns>Vector3 position of given joint</returns>
    public Vector3 GetJointPos(int player, KinectWrapper.Joints joint) {

        KinectWrapper.SkeletonTransform trans = new KinectWrapper.SkeletonTransform();
        if (player == 1 || player == 2) {
            KinectWrapper.GetSkeletonTransform(player, (int)joint, ref trans);
            return (new Vector3(trans.x * scaleFactor, trans.y * scaleFactor, trans.z * scaleFactor));
        } else {
            return Vector3.zero;
        }
    }


    /// <summary>
    /// one-player override of joint position get function
    /// </summary>
    /// <param name="joint">KinectWrapper.Joints enum</param>
    /// <returns>position of given joint</returns>
    public Vector3 GetJointPos(KinectWrapper.Joints joint) {

        return (GetJointPos(1, joint));
    }


    /// <summary>
    /// gets color texture image from Kinect RGB camera
    /// </summary>
    /// <returns>RGB image</returns>
    public Texture2D GetTextureImage() {

        return this.texture;
    }


    /// <summary>
    /// gets depth data from Kinect depth camera
    /// </summary>
    /// <returns>2D array of pixel depths as bytes</returns>
    /// <remarks>depth[x=0][y=0] corresponds to top-left corner of image</remarks>
    public byte[][] GetDepthData() {

        return this.depth;
    }


    /// <summary>
    /// returns current Kinect camera angle from horizontal
    /// </summary>
    /// <returns>camera angle from horizontal</returns>
    public float GetCameraAngle() {

        float cameraAngle = 0;

        KinectWrapper.GetCameraAngle(ref cameraAngle);
        return (cameraAngle);
    }


    /// <summary>
    /// sets Kinect camera angle
    /// </summary>
    /// <param name="angle">range: -27 -> 27</param>
    /// <returns>returns true if successful</returns>
    /// <remarks>do not change angle more than once every 30 sec</remarks>
    public bool SetCameraAngle(int angle) {

        /* DO NOT CHANGE CAMERA ANGLE MORE OFTEN THAN ONCE
         * EVERY 30 SECONDS, IT COULD DAMAGE THE KINECT
         * SENSOR (SEE KINECT SDK DOCUMENTATION).
         */

        if (Time.time - lastCameraAngleChange > 30) {
            lastCameraAngleChange = Time.time;
            return (KinectWrapper.SetCameraAngle(angle));
        } else {
            return (false);
        }
    }


/********************************************************************************
*       DEVICE MANAGEMENT -> initialize, uninitialize, and update sensor
* ******************************************************************************/

    //called on application start
    private void Start() {

        NUIisReady = false;
		audioIsReady = false;
		//audioIsReady = KinectWrapper.AudioContextInit();

        //initialize Kinect sensor
        NUIisReady = KinectWrapper.NuiContextInit(twoPlayer);

        //display messages
        if (NUIisReady)
            Debug.Log("Sensor Initialized.");
        else
            Debug.Log("Could Not Initialize Sensor.");

		// if (audioIsReady)
			// Debug.Log("Audio Initialized.");
		// else
			// Debug.Log("Could Not Initialize Audio Source.");
			
        if (scaleFactor == 0) {
            Debug.Log("WARNING: KUInterface.scaleFactor is set to zero. All joint positions will be the zero vector.");
        }

        //set up image memory
        if (useRGB) {
            seqTex = new byte[IM_W * IM_H * 4];
            cols = new Color32[IM_W * IM_H];
            texture = new Texture2D(IM_W, IM_H);
        } else {
            displayTextureImage = false;
        }

        if (useDepth) {
            seqDepth = new byte[IM_W * IM_H * 2];
            dcols = new Color32[IM_W * IM_H];
            depthImg = new Texture2D(IM_W, IM_H);
            depth = new byte[IM_W][];
            for (int i = 0; i < depth.Length; i++) {
                depth[i] = new byte[IM_H];
            }
        } else {
            displayDepthImage = false;
        }
		
		motor = GetComponent<CharacterMotor>();
		
		player = GameObject.FindGameObjectWithTag("Player");
		katana = GameObject.Find("Katana");
		
		
    }


    //called on application stop
    private void OnApplicationQuit() {

        //if Unity is in editor mode, the Kinect sensor will remain active
        //if (!Application.isEditor) {
            KinectWrapper.NuiContextUnInit();
			//KinectWrapper.AudioContextUnInit();
        //}
    }

	float alpha = 0.1f;
	int tau = 3;
	Vector3 pPredictionR = new Vector3(0,0,0);
	Vector3 spLastR = new Vector3(0,0,0);
	Vector3 s2pLastR = new Vector3(0,0,0);
	
	Vector3 pPredictionL = new Vector3(0,0,0);
	Vector3 spLastL = new Vector3(0,0,0);
	Vector3 s2pLastL = new Vector3(0,0,0);
	
	GameObject[] gos;
	GameObject player;
	GameObject katana;
	
	private void UpdateDESP(){
		Vector3 pCurrentR = GetJointPos(KinectWrapper.Joints.HAND_RIGHT);
		Vector3 spCurrentR = alpha * pCurrentR + (1.0f-alpha) * spLastR;
		Vector3	s2pCurrentR = alpha * spCurrentR + (1.0f-alpha) * s2pLastR;
		spLastR = spCurrentR;
		s2pLastR = s2pCurrentR;
		float alphaTerm = alpha * tau / (1.0f-alpha);
		pPredictionR = (2 + alphaTerm) * spCurrentR - (1 + alphaTerm) * s2pCurrentR; 
		
		Vector3 pCurrentL = GetJointPos(KinectWrapper.Joints.HAND_LEFT);
		Vector3 spCurrentL = alpha * pCurrentL + (1.0f-alpha) * spLastL;
		Vector3	s2pCurrentL = alpha * spCurrentL + (1.0f-alpha) * s2pLastL;
		spLastL = spCurrentL;
		s2pLastL = s2pCurrentL;
		pPredictionL = (2 + alphaTerm) * spCurrentL - (1 + alphaTerm) * s2pCurrentL; 
	}
	
	
    //called every Unity frame (frame-rate dependent)
    private void Update() {

        //update Kinect
        KinectWrapper.NuiUpdate();

        //update RGB texture
        if (useRGB) {
            UpdateTextureImage();
        }

        //update Depth data
        if (useDepth) {
            UpdateDepth();
        }
		
		if(started){
			
			Vector3 rightHandPos = pPredictionR;
			Vector3 leftHandPos = pPredictionL;
			Vector3 handVec = rightHandPos - leftHandPos;
			float magHandVec = handVec.magnitude;
			
			Vector3 rightForearmVec = rightHandPos-GetJointPos(KinectWrapper.Joints.ELBOW_RIGHT);
			Vector3 rightArmVec = rightHandPos-GetJointPos(KinectWrapper.Joints.SHOULDER_RIGHT);
			rightForearmVec.Normalize();
			rightArmVec.Normalize();
			
			Vector3 leftForearmVec = leftHandPos-GetJointPos(KinectWrapper.Joints.ELBOW_LEFT);
			Vector3 leftArmVec = leftHandPos-GetJointPos(KinectWrapper.Joints.SHOULDER_LEFT);
			leftForearmVec.Normalize();
			leftArmVec.Normalize();
			
			if(magHandVec > 0.0f && magHandVec <= 0.25f){
				//Debug.Log("Katana");
				katana.SetActiveRecursively(true);
			}
			
			transform.position = leftHandPos + new Vector3(0,2.0f,0);
			handVec.Normalize();
			transform.rotation = 
				Quaternion.Slerp(
					transform.rotation, 
					Quaternion.LookRotation(Vector3.Scale(handVec, new Vector3(1,1,-1))),
					Time.deltaTime*5);
			
			
			Vector3 rightFoot = GetJointPos(KinectWrapper.Joints.FOOT_RIGHT);
			Vector3 leftFoot = GetJointPos (KinectWrapper.Joints.FOOT_LEFT);
			Vector3 rightShoulder = GetJointPos (KinectWrapper.Joints.SHOULDER_RIGHT);
			Vector3 leftShoulder = GetJointPos (KinectWrapper.Joints.SHOULDER_LEFT);
			
			// Save initial feet and shoulder height for use in jump and duck
			if(!savedData){
				baseFeetHeight = rightFoot;
				baseHeadHeight = rightShoulder;
				savedData = true;
			}
			
			// Forward/Backward Movement
			Vector3 footDirection = rightFoot-leftFoot;
			if(Vector3.Dot(-Vector3.forward, footDirection.normalized) > 0.8){
				motor.inputMoveDirection = transform.rotation * Vector3.forward;	
			}
			else if(Vector3.Dot(Vector3.forward, footDirection.normalized) > 0.8){
				motor.inputMoveDirection = transform.rotation * -Vector3.forward;	
			}
			else{
				motor.inputMoveDirection = Vector3.zero;	
			}
			
			// Turning Movement
			Vector3 shoulderDirection = rightShoulder - leftShoulder;
			if(Vector3.Dot(new Vector3(0.5f, 0 , -1), shoulderDirection.normalized) > 0.8){
				transform.Rotate(0, -1.0f, 0);	
			}
			else if(Vector3.Dot(new Vector3(0.5f, 0 , 1), shoulderDirection.normalized) > 0.8){
				transform.Rotate(0, 1.0f, 0);	
			}
			
			// Jump and Duck Movement
			Debug.Log((rightFoot-baseFeetHeight).y);
			motor.inputJump = ((rightFoot-lastRightFoot).y > 5.0f && (leftFoot-lastLeftFoot).y > 5.0f);	
			lastRightFoot = rightFoot;
			lastLeftFoot = leftFoot;
		}
		else if(Vector3.Dot(Vector3.right, (GetJointPos(KinectWrapper.Joints.HAND_RIGHT) - GetJointPos(KinectWrapper.Joints.HEAD)).normalized) > 0.8){
			started = true;
		}
		UpdateDESP();
    }


    //update RGB texture
    private void UpdateTextureImage() {

        int size = 0;

        //copy pixel data from unmanaged memory
        IntPtr ptr = KinectWrapper.GetTextureImage(ref size);
        Marshal.Copy(ptr, seqTex, 0, size);

        //create color matrix
        for (int i = 0; i < (IM_W * IM_H * 4); i += 4) {
            cols[(IM_W * IM_H) - (i / 4) - 1] = new Color32(seqTex[i + 2], seqTex[i + 1], seqTex[i], 255);
        }

        //set texture
        texture.SetPixels32(cols);
        texture.Apply();
    }


    //update depth data
    private void UpdateDepth() {

        // int size = 0;

        // //copy pixel data from unmanaged memory
        // IntPtr ptr = KinectWrapper.GetDepthImage(ref size);
        // Marshal.Copy(ptr, seqDepth, 0, size);

        // //create depth array
        // for (int y = 0; y < IM_H; y++) {
            // for (int x = 0; x < IM_W; x++) {
                // depth[x][y] = seqDepth[(IM_H * IM_W * 2) - (IM_W * 2 * y) - ((x * 2) + 1)];
            // }
        // }

        // if (displayDepthImage) {
            // //create color matrix
            // for (int y = 0; y < IM_H; y++) {
                // for (int x = 0; x < IM_W; x++) {
                    // dcols[x + (y * IM_W)] = new Color32(0, depth[x][y], 0, 255);
                // }
            // }
            
            // //set texture
            // depthImg.SetPixels32(dcols);
            // depthImg.Apply();
        // }
    }


    //update function for GUI (if enabled)
    private void OnGUI() {

        if (displayJointInformation) {
            //display joint info in upper-left corner
            if (twoPlayer) {
                for (int i = 0; i < (int)KinectWrapper.Joints.COUNT; i++) {
                    DisplayPlayerData(1, i);
                    DisplayPlayerData(2, i);
                }
            } else {
                for (int i = 0; i < (int)KinectWrapper.Joints.COUNT; i++) {
                    DisplayPlayerData(1, i);
                }
            }
        }

        if (displayTextureImage && useRGB) {
            //scaled to half-res
            GUI.DrawTexture(new Rect(600, 50, IM_W / 2, IM_H / 2), texture, ScaleMode.ScaleToFit, false);
        }

        if (displayDepthImage && useDepth) {
            //scaled to half-res
            GUI.DrawTexture(new Rect(600, 300, IM_W / 2, IM_H / 2), depthImg, ScaleMode.ScaleToFit, false);
        }
    }


    //displays joint position data in GUI (if enabled)
    private void DisplayPlayerData(int player, int place) {

        Vector3 joint = GetJointPos(player, (KinectWrapper.Joints)place);

        GUI.Label(new Rect((player - 1) * 300, place * 30, 200, 30), joint.ToString());
    }
}


//------------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------------

/// <summary>
/// KUInterface.dll wrapper
/// </summary>
public class KinectWrapper {  //interfaces with DLL

    /// <summary>
    /// defines Kinect skeleton
    /// </summary>
    public enum Joints {
        HIP_CENTER = 0,
        SPINE,
        SHOULDER_CENTER,
        HEAD,
        SHOULDER_LEFT,
        ELBOW_LEFT,
        WRIST_LEFT,
        HAND_LEFT,
        SHOULDER_RIGHT,
        ELBOW_RIGHT,
        WRIST_RIGHT,
        HAND_RIGHT,
        HIP_LEFT,
        KNEE_LEFT,
        ANKLE_LEFT,
        FOOT_LEFT,
        HIP_RIGHT,
        KNEE_RIGHT,
        ANKLE_RIGHT,
        FOOT_RIGHT,
        COUNT
    };


    [StructLayout(LayoutKind.Sequential)]
    public struct SkeletonTransform {

        public float x, y, z, w;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct KUVector4 {

        public float x, y, z, w;
    }


    //NUI Context Management
    [DllImport("/Assets/Kinect/Plugins/KUInterface.dll")]
    public static extern bool NuiContextInit(bool twoPlayer);
    [DllImport("/Assets/Kinect/Plugins/KUInterface.dll")]
    public static extern void NuiUpdate();
    [DllImport("/Assets/Kinect/Plugins/KUInterface.dll")]
    public static extern void NuiContextUnInit();
	//Audio Context Management
	// [DllImport("/Assets/Kinect/Plugins/KUInterface.dll")]
	// public static extern bool AudioContextInit();
	// [DllImport("/Assets/Kinect/Plugins/KUInterface.dll")]
	// public static extern void AudioContextUnInit();
	
	
	
    //Get Methods
    [DllImport("/Assets/Kinect/Plugins/KUInterface.dll")]
    public static extern void GetSkeletonTransform(int player, int joint, ref SkeletonTransform trans);
    [DllImport("/Assets/Kinect/Plugins/KUInterface.dll")]
    public static extern IntPtr GetTextureImage(ref int size);
    [DllImport("/Assets/Kinect/Plugins/KUInterface.dll")]
    public static extern IntPtr GetDepthImage(ref int size);
    [DllImport("/Assets/Kinect/Plugins/KUInterface.dll")]
    public static extern void GetCameraAngle(ref float angle);
    //Set Methods
    [DllImport("/Assets/Kinect/Plugins/KUInterface.dll")]
    public static extern bool SetCameraAngle(int angle);
}
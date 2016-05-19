using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AVOSCloud;
using Divine.AwesomeAds.List;

namespace Divine.AwesomeAds{
	#if UNITY_EDITOR
	public static class EditorUtitlity{
		[MenuItem ("Tools/Delete All Player Prefabs")]
		public static void DeleteAllPlayerPrefs ()
		{
			PlayerPrefs.DeleteAll ();
		}
	}
	#endif
	public class AwesomeAdsManager : MonoBehaviour {
		public static bool Inited = false;
		public static AwesomeAdsManager Instance;

		public const string BaseIconURL = "https://db.tt/"; // Dropbox
		#if UNITY_EDITOR
		public static string BaseLocalPath = Application.dataPath + "/../AwesomeAds/";
		#else
		public static string BaseLocalPath = Application.persistentDataPath + "/AwesomeAds/";
		#endif

		public static List<AwesomeAdsListData> ListData{
			get{
				return Instance.m_CombinedListData;
			}
		}

		public static bool UIEnable{
			set{
				if (value)
					Instance.GetComponentInChildren<AwesomeAdsList> ().EnableCanvas ();
				else
					Instance.GetComponentInChildren<AwesomeAdsList> ().DisableCanvas ();
			}
		}

		public UnityEngine.Events.UnityEvent OnLoadFinish;

		public string m_BundleID;

		public List<AwesomeAdsListData> m_DefaultAdsListData; // Default list data

		List<AwesomeAdsData> m_AdsData = new List<AwesomeAdsData> (); // Raw data from server
		List<Texture2D> m_Icons = new List<Texture2D> ();
		List<AwesomeAdsListData> m_AdsListData = new List<AwesomeAdsListData> (); // Optimized data used for list

		List<AwesomeAdsListData> m_CombinedListData; // CombinedListData

		void Awake ()
		{
			if (Instance == null)
				Instance = this;
			else if (Instance != this)
				Destroy (this.gameObject);

			DontDestroyOnLoad (this.gameObject);

		}

		void Start ()
		{
			if (PlayerPrefs.HasKey ("AwesomeAdsInited"))
			{
				Debug.Log ("Awesome Ads has been inited before. Init will be skipped.");
				if (PlayerPrefs.HasKey ("AwesomeAdsSuccess"))
				{
					//	Load data from local storage
					AdsFileSystem.Load ();
					m_AdsData = AdsFileSystem.AwesomeAds.Data.ToList ();
					StartCoroutine (GetLocalList ());
				}else
				{
					Inited = true;
					OnLoadFinish.Invoke ();
				}
			}
			else
			{
				PlayerPrefs.SetInt ("AwesomeAdsInited", 1);
				StartCoroutine (GetAwesomeAppList ());
			}
		}

		#region Remote Data Sync
		void CreateDir ()
		{
			if (!Directory.Exists (BaseLocalPath))
				Directory.CreateDirectory (BaseLocalPath);
		}

		IEnumerator GetAwesomeAppList ()
		{
			CreateDir ();

			var query = new AVQuery<AVObject> ("AwesomeClass");
			query.WhereEqualTo ("BundleId", m_BundleID);
			query.OrderBy ("Index");

			var task = query.FindAsync ();
			while (!task.IsCompleted)
				yield return null;

			if (task.IsFaulted || task.IsCanceled)
			{
				Debug.Log ("Error initializing Awesome Ads : " + task.Exception.InnerExceptions[0].Message);
				Inited = true;
				yield break;
			}

			Debug.Log ("Data fetch successfully");

			foreach (AVObject itemInfo in task.Result)
			{
				m_AdsData.Add (new AwesomeAdsData (
					itemInfo.Get<string> ("AppName"),
					itemInfo.Get<string> ("BundleId"),
					itemInfo.Get<string> ("RedirectURL"),
					itemInfo.Get<AVFile> ("IconFile").Url.AbsoluteUri,
					itemInfo.Get<int> ("Index"))
				);
			}

			// Get All Icons
			for (int i=0; i<m_AdsData.Count; i++)
			{
				WWW www = new WWW (m_AdsData[i].IconURL);
				yield return www;
				if (!string.IsNullOrEmpty (www.error))
				{
					m_AdsData[i].LocalIconPath = string.Empty;
					Debug.Log ("Error get icon : "+ m_AdsData[i].IconURL+", "+www.error);
					m_Icons.Add (default (Texture2D));
					continue;
				}
				Debug.Log ("Return icon image");
				m_AdsData[i].LocalIconPath 
				= BaseLocalPath+m_AdsData[i].BundleId+"_"+m_AdsData[i].Index.ToString ()+".png";
				m_Icons.Add (www.texture);

				// Save icon locally
				byte[] pngBuffer = www.texture.EncodeToPNG ();
				File.WriteAllBytes (m_AdsData[i].LocalIconPath, pngBuffer);
			}

			// Save data structure locally
			AdsFileSystem.AwesomeAds = new J_AwesomeAds (m_AdsData);
			AdsFileSystem.Save ();
			PlayerPrefs.SetInt ("AwesomeAdsSuccess", 1);

			CreateAwesomeAdsListData ();
			yield return null;
		}
		#endregion

		#region Load Local Data

		IEnumerator GetLocalList ()
		{
			for (int i=0; i<m_AdsData.Count; i++)
			{
				if (m_AdsData[i].LocalIconPath == string.Empty)
				{
					m_Icons.Add (default (Texture2D));
				}
				WWW www = new WWW ("file://"+ m_AdsData[i].LocalIconPath);
				yield return www;
				if (!string.IsNullOrEmpty (www.error))
				{
					Debug.Log ("Error loading local icon : "+ m_AdsData[i].LocalIconPath);
					m_Icons.Add (default (Texture2D));
					continue;
				}
//				Texture2D tex = new Texture2D (128,128,TextureFormat.ARGB32,false);
//				tex.LoadImage (www.bytes);
//				m_Icons.Add (tex);
				m_Icons.Add (www.texture);
			}

			CreateAwesomeAdsListData ();
			yield return null;
		}

		#endregion

		#region Create Ads List Data

		void CreateAwesomeAdsListData ()
		{
			for (int i=0; i<m_AdsData.Count; i++)
				m_AdsListData.Add (new AwesomeAdsListData (
					m_AdsData[i].AppName,
					m_AdsData[i].RedirectURL,
					m_AdsData[i].Index,
					m_Icons[i]
				));

			// Combine default and server list data
			m_CombinedListData = new List<AwesomeAdsListData> ();
			foreach (AwesomeAdsListData defaultListdata in m_DefaultAdsListData)
			{
				foreach (AwesomeAdsListData serverListData in m_AdsListData)
					defaultListdata.TryReplace (serverListData);
				m_CombinedListData.Add (defaultListdata);
			}

			Inited = true;
			OnLoadFinish.Invoke ();
		}

		#endregion
	}
}
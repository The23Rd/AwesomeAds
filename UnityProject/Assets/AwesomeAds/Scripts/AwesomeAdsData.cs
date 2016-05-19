using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Divine.AwesomeAds{
	[System.Serializable]
	public class AwesomeAdsData {

		public string AppName;
		public string BundleId;
		public string RedirectURL;
		public string IconURL;
		public int Index;
		public string LocalIconPath;

		public AwesomeAdsData (string appName, string bundleId, string redirectURL, string iconURL, int index)
		{
			AppName = appName;
			BundleId = bundleId;
			RedirectURL = redirectURL;
			IconURL = iconURL;
			Index = index;
		}
	}

	[System.Serializable]
	public struct AwesomeAdsListData {
		public string Name;
		public string RedirectURL;
		public int Index;
		public Sprite Icon;

		#region constructor
		public AwesomeAdsListData (string name, string url, int index, Texture2D texture)
		{
			Name = name;
			RedirectURL = url;
			Index = index;
			if (texture == default (Texture2D))
				Icon = null;
			else
				Icon = Sprite.Create (texture, new Rect (0,0,texture.width,texture.height), new Vector2 (0.5f,0.5f));
		}
		#endregion

		public void TryReplace (AwesomeAdsListData dataToReplace)
		{
			if (dataToReplace.Index != Index)
				return;
			Name = dataToReplace.Name;
			RedirectURL = dataToReplace.RedirectURL;
			Icon = dataToReplace.Icon;

			Debug.Log ("Replace list index : "+Index);
		}
	}

	#region JSON Warpper

	[System.Serializable]
	public struct J_AwesomeAds{
		public AwesomeAdsData[] Data;

		public J_AwesomeAds (List<AwesomeAdsData> data)
		{
			Data = new AwesomeAdsData[data.Count];
			for (int i=0; i<data.Count; i++)
				Data[i] = data[i];
		}
	}

	#endregion

	public class AdsFileSystem{
		public static J_AwesomeAds AwesomeAds;
		static string JSON;

		public static void Save ()
		{
			JSON = JsonUtility.ToJson(AwesomeAds);
			Debug.Log ("JSON Saving  >>> "+JSON);
			using (FileStream fs = new FileStream (AwesomeAdsManager.BaseLocalPath + "data.json", FileMode.Create))
			{
				using (StreamWriter writer = new StreamWriter (fs))
				{
					writer.Write (JSON);
				}
			}
		}

		public static void Load ()
		{
			using (FileStream fs = new FileStream (AwesomeAdsManager.BaseLocalPath + "data.json", FileMode.Open))
			{
				using (StreamReader reader = new StreamReader (fs))
				{
					JSON = reader.ReadToEnd ();
				}
			}
			AwesomeAds = JsonUtility.FromJson<J_AwesomeAds> (JSON);
		}
	}
}
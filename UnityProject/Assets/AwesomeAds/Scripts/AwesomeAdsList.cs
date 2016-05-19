using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Divine.AwesomeAds.List{
	public class AwesomeAdsList : MonoBehaviour {
		public static bool Loaded = false;

		[SerializeField] Canvas m_Canvas;
		[SerializeField] CanvasGroup m_CanvasGroup;
		[SerializeField] GameObject m_LoadingGO;
		[SerializeField] Transform m_ListItemPrefab;

		Coroutine m_coroutine;
		public void EnableCanvas ()
		{
			m_Canvas.enabled = true;
		}

		public void DisableCanvas ()
		{
			m_Canvas.enabled = false;
		}

		public void Open ()
		{
			m_CanvasGroup.alpha = 1;
			m_CanvasGroup.blocksRaycasts = true;

			if (!Loaded)
			{
				m_LoadingGO.SetActive (true);

				// Create List Items..
				m_coroutine = StartCoroutine (CreateListItems ());
			}
		}

		IEnumerator CreateListItems ()
		{
			float time = Time.time;
			while (!AwesomeAdsManager.Inited)
			{
				if (Time.time - time >= 15f) // Timeout after 15 sec
					break;
				yield return null;
			}

			m_LoadingGO.SetActive (false);
			for (int i=0; i<AwesomeAdsManager.ListData.Count; i++)
			{
				Transform newItem = Instantiate (m_ListItemPrefab);
				newItem.SetParent (transform, false);
				newItem.GetComponent<AwesomeAdsListItem> ().Setup (AwesomeAdsManager.ListData[i]);
			}

			Loaded = true;
			yield return null;
			m_coroutine = null;
		}

		public void Close ()
		{
			if (m_coroutine != null)
				StopCoroutine (m_coroutine);
			m_CanvasGroup.alpha = 0;
			m_CanvasGroup.blocksRaycasts = false;
		}

		public void OnMenuSwitch (bool isOn)
		{
			if (isOn)
				Open ();
			else
				Close ();
		}
	}
}
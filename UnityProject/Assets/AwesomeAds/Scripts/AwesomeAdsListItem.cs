using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Divine.AwesomeAds.List{
	public class AwesomeAdsListItem : MonoBehaviour {

		[SerializeField] Image m_Icon;
		[SerializeField] Text m_Title;
		[SerializeField] Button m_Button;
		string m_URL;

		public void Setup (AwesomeAdsListData listData)
		{
			m_Icon.sprite = listData.Icon;
			m_Title.text = listData.Name;
			m_URL = listData.RedirectURL;

			m_Button.onClick.RemoveAllListeners ();
			m_Button.onClick.AddListener (()=>{
				Application.OpenURL (m_URL);
			});
			gameObject.SetActive (true);
		}
	}
}
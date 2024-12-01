using System.Collections.Generic;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using XWear.IO.Runtime.Components.AccessoryRoot;
using XWear.IO.Runtime.Components.HumanoidMap;

namespace pspkurara.VRM10FromXRoidAvatarFixer.Runtime
{

	/// <summary>
	/// XWearの参照元オブジェクト
	/// XWearの書き出し前プレハブを格納する
	/// </summary>
	public sealed class XWearSource : ScriptableObject
	{

		[Header("XWear Before Export Source Prefabs")]
		[SerializeField] private List<XWearData> m_Wears;

		/// <summary>
		/// XWearオブジェクトの参照
		/// </summary>
		public List<XWearData> wears { get { return m_Wears; } }

	}

	/// <summary>
	/// XWearごとの個別
	/// </summary>
	[System.Serializable]
	public sealed class XWearData
	{

		[Header("Source Prefab")]
		[SerializeField] private GameObject m_Wear;

		[Header("Object Name In Avatar")]
		[SerializeField] private string m_Name;

		/// <summary>
		/// 書き出し前のXWearプレハブ
		/// これを基に生成する
		/// </summary>
		public GameObject wear { get { return m_Wear; } }

		/// <summary>
		/// オブジェクトの名前
		/// 空にしてあるとプレハブの名前を使うようになる
		/// </summary>
		public string name { get { return string.IsNullOrEmpty(m_Name) ? m_Wear.name : m_Name; } }

		/// <summary>
		/// XWearがアクセサリーか
		/// </summary>
		public bool isAccessoryRoot { get { return GetAccessoryRootComponent(); } }

		/// <summary>
		/// XWearが衣装か
		/// </summary>
		public bool isHumanoidMap { get { return GetHumanoidMapComponent(); } }

		/// <summary>
		/// PhysBoneを持っているか
		/// </summary>
		public bool hasPhysBone
		{
			get
			{
				return wear.GetComponentInChildren<VRCPhysBone>() != null;
			}
		}

		/// <summary>
		/// Colliderを持っているか
		/// </summary>
		public bool hasCollider
		{
			get
			{
				return wear.GetComponentInChildren<VRCPhysBoneCollider>() != null;
			}
		}

		/// <summary>
		/// アクセサリーのルートコンポーネントを取得
		/// </summary>
		/// <returns>ルートコンポーネント</returns>
		public AccessoryRootComponent GetAccessoryRootComponent()
		{
			return wear.GetComponent<AccessoryRootComponent>();
		}

		/// <summary>
		/// 衣装のルートコンポーネントを取得
		/// </summary>
		/// <returns>ルートコンポーネント</returns>
		public HumanoidMapComponent GetHumanoidMapComponent()
		{
			return wear.GetComponent<HumanoidMapComponent>();
		}

	}


}
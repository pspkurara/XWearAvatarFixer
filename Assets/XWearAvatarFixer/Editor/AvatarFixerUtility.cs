using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UniVRM10;
using VRC.Dynamics;

namespace pspkurara.VRM10FromXRoidAvatarFixer.Editor
{
    public static class AvatarFixerUtility
	{

		public static readonly char Separater = System.IO.Path.AltDirectorySeparatorChar;

		/// <summary>
		/// 親から子までのTransformのパスを取得する
		/// </summary>
		/// <param name="parent">親</param>
		/// <param name="child">子</param>
		/// <returns>パス</returns>
		public static string GetTransformPath(Transform parent, Transform child)
		{
			StringBuilder sb = new StringBuilder();
			while (parent != child || child == null)
			{
				sb.Insert(0, child.name);
				sb.Insert(0, Separater);
				child = child.parent;
			}
			sb.Insert(0, parent.name);
			return sb.ToString();
		}

		/// <summary>
		/// 各最下層までのTransformツリーを取得する
		/// </summary>
		/// <param name="t">トップ</param>
		/// <param name="caches">見つけたツリーを格納するリスト</param>
		public static void GetDeepTransform(Transform t, List<Transform> caches)
		{
			if (t.childCount == 0)
			{
				caches.Add(t);
				return;
			}
			for (int i = 0; i < t.childCount; i++)
			{
				GetDeepTransform(t.GetChild(i), caches);
			}
		}

		/// <summary>
		/// VRM10のシェイプタイプに変換
		/// </summary>
		/// <returns>シェイプタイプ</returns>
		public static VRM10SpringBoneColliderTypes ToVrm10(this VRCPhysBoneColliderBase.ShapeType t)
		{
			switch (t)
			{
				case VRCPhysBoneColliderBase.ShapeType.Sphere: return VRM10SpringBoneColliderTypes.Sphere;
				case VRCPhysBoneColliderBase.ShapeType.Capsule: return VRM10SpringBoneColliderTypes.Capsule;
				case VRCPhysBoneColliderBase.ShapeType.Plane: return VRM10SpringBoneColliderTypes.Plane;
			}
			return default;
		}


	}

	/// <summary>
	/// パスキャッシュ用構造体
	/// </summary>
	public struct PathGroup : IEqualityComparer<PathGroup>, IEquatable<PathGroup>
	{
		/// <summary>
		/// パス一覧
		/// </summary>
		public List<string> pathes;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="root">パスのルートオブジェクト</param>
		/// <param name="cols">子にあるコライダー</param>
		public PathGroup(Transform root, IEnumerable<VRCPhysBoneColliderBase> cols)
		{
			pathes = cols
				.Select(c => AvatarFixerUtility.GetTransformPath(root, c.transform))
				.ToList();
			pathes.Sort();
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="root">パスのルートオブジェクト</param>
		/// <param name="list">子にあるコライダー</param>
		public PathGroup(Transform root, IEnumerable<VRM10SpringBoneColliderGroup> list)
		{
			pathes = list
				.SelectMany(l => l.Colliders)
				.Select(l => AvatarFixerUtility.GetTransformPath(root, l.transform))
				.ToList();
			pathes.Sort();
		}

		public bool Equals(PathGroup x, PathGroup y)
		{
			// パスでの一致
			return x.ToString().Equals(y.ToString());
		}

		public bool Equals(PathGroup other)
		{
			return Equals(this, other);
		}

		public int GetHashCode(PathGroup obj)
		{
			return GetHashCode() ^ obj.GetHashCode();
		}

		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}

		public override string ToString()
		{
			// パスを全てリストアップ
			StringBuilder b = new StringBuilder();
			foreach (var o in pathes)
			{
				b.Append(o);
			}
			return b.ToString();
		}

	}

}

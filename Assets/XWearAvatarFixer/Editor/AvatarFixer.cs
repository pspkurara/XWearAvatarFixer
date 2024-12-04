using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UniVRM10;
using VRC.SDK3.Dynamics.PhysBone.Components;
using UniHumanoid;

namespace pspkurara.VRM10FromXRoidAvatarFixer.Editor
{
	using Runtime;
	using VRC.PackageManagement.Core;
	using XWear.IO.Runtime.Components.AccessoryRoot;
	using XWear.IO.Runtime.Components.HumanoidMap;

	/// <summary>
	/// アバターを修正する
	/// </summary>
	public static class AvatarFixer
	{

		/// <summary>
		/// XWearのアセット参照
		/// </summary>
		private static XWearSource wearSource
		{
			get
			{
				if (wearSourceCache != null) return wearSourceCache;
				// XWearSourceのオブジェクトはAssetsフォルダに1つしかないという想定
				var find = AssetDatabase.FindAssets($"t:{typeof(XWearSource).Name}", new string[] { "Assets" });
				var r = find.FirstOrDefault();
				if (r == null) return null;
				r = AssetDatabase.GUIDToAssetPath(r);
				wearSourceCache = AssetDatabase.LoadAssetAtPath<XWearSource>(r);
				return wearSourceCache;
			}
		}

		private static XWearSource wearSourceCache;

		/// <summary>
		/// XWearSourceが存在するかを返す
		/// </summary>
		/// <returns>存在する</returns>
		public static bool IsExsitsXWearSource()
		{
			return wearSource != null;
		}


		/// <summary>
		/// VRM1.0 SpringBoneごとの処理
		/// </summary>
		/// <param name="vrmInstance">VRMアバターオブジェクト</param>
		public static void ConvertSpringBone(Vrm10Instance vrmInstance)
		{
			Undo.RecordObject(vrmInstance, "UpdateInfos");

			// VRM1.0アバターの管理コンポーネント類を取得
			Vrm10InstanceSpringBone bones = vrmInstance.SpringBone;
			Humanoid humanoid = vrmInstance.GetComponent<Humanoid>();

			// アクセサリー
			var accessoryWears = wearSource.wears
				.Where(w => w.isAccessoryRoot);

			// 衣装
			var humanoidMapWears = wearSource.wears
				.Where(w => w.isHumanoidMap);

			// Springごとに処理する
			foreach (var b in bones.Springs)
			{
				// リストの一番頭がツリーの一番上に来るようだ
				var rootJoint = b.Joints.First();

				// 該当のJointと一致する階層構造のアクセサリを探す
				XWearData targetWear = accessoryWears.FirstOrDefault(w => IsUsingAccessoryRootComponentWear(rootJoint, w));
				if (targetWear != null)
				{
					// 見つけたら処理する
					ConvertSpringJoint(b, targetWear.GetAccessoryRootComponent());
					continue;
				}

				// 該当のJointと一致する階層構造の衣装を探す
				targetWear = humanoidMapWears.FirstOrDefault(w => IsUsingHumanoidMapWear(rootJoint, vrmInstance, w));
				if (targetWear != null)
				{
					// 見つけたら処理する
					ConvertSpringJoint(humanoid, b, targetWear.GetHumanoidMapComponent());
					continue;
				}

			}

			// 当たり判定を持つ衣装を抽出する
			var collisionWears = wearSource.wears
				.Where(w => w.hasCollider)
				// TODO: 生成失敗は現状だと衣装のみで確認できたので、アクセサリーはとりあえず後回しにする
				.Where(w => bones.Springs.Any(b => {
					var rootJoint = b.Joints.First();
					return IsUsingHumanoidMapWear(rootJoint, vrmInstance, w);
					}
				));

			// secondaryというオブジェクトにコライダーグループが取り付けられるようだ
			Transform secondary = vrmInstance.transform.Find("secondary");

			// ボーンツリーのトップとなる場所
			// Hip以上には取り付けられないだろうという前提
			const HumanBodyBones rootBoneKey = HumanBodyBones.Hips;

			// アバター側の親ボーンを取得
			Transform rootBone = humanoid.GetBoneTransform(rootBoneKey);

			// 現在同じコライダーの組み合わせを持っているかをチェックし、キャッシュしておく
			// VRM1.0はコライダーグループというものが使われている
			// PhysBoneはそのような中間がないので基本的には新規の中間は1つにまとめる前提とする
			Dictionary<PathGroup, List<VRM10SpringBoneColliderGroup>> currentColliders = bones.Springs
													.GroupBy(s => new PathGroup(rootBone, s.ColliderGroups))
													.Select(group => group.First()) // 最初のキーとペアを保持
													.ToDictionary(kvp => new PathGroup(rootBone, kvp.ColliderGroups), kvp => kvp.ColliderGroups.ToList());

			// 元XWearと比較して、コライダーが不足している箇所があれば生成する
			foreach (var cw in collisionWears)
			{

				// XWearオブジェクトのルートボーンを取得
				// キーを合わせることでパスを一致させる
				// 衣装の階層構造はVroidの通常ボーンのものと変わらないという前提にしてある (Hipsがなければそれ以下も追従させるのが難しいと思うので)
				var wearRootBone = cw.GetHumanoidMapComponent().HumanoidMap.GetMap.FindFirstKeyByValue(rootBoneKey);
				// PhysBoneからコライダーを持つものだけを取り出して
				var uniqueColliderSet = cw.wear.GetComponentsInChildren<VRCPhysBone>()
					.Where(w => w.colliders.Count > 0)
					.Select(w => new Tuple<PathGroup, string>(new PathGroup(wearRootBone, w.colliders), AvatarFixerUtility.GetTransformPath(wearRootBone, w.GetRootTransform())));

				foreach (var u in uniqueColliderSet)
				{

					// まだ存在しないコライダーグループである
					if (!currentColliders.ContainsKey(u.Item1))
					{
						// VRMのコライダーグループを作る
						var createdColliderGroup = Undo.AddComponent<VRM10SpringBoneColliderGroup>(secondary.gameObject);
						foreach (var c in u.Item1.pathes)
						{
							// 親からパスをたどって一番下に行く
							Transform currentTarget = rootBone.parent;
							Transform physBoneTarget = wearRootBone.parent;
							foreach (var dir in c.Split(AvatarFixerUtility.Separater))
							{
								currentTarget = currentTarget.Find(dir);
								physBoneTarget = physBoneTarget.Find(dir);
							}
							// 当たり判定コンポーネントを作成
							var collider = Undo.AddComponent<VRM10SpringBoneCollider>(currentTarget.gameObject);
							var physBoneCollider = physBoneTarget.GetComponent<VRCPhysBoneCollider>();

							// 設定を上書きしていく
							collider.ColliderType = physBoneCollider.shapeType.ToVrm10();
							collider.Radius = physBoneCollider.radius;

							// ローカルY軸の向きベクトルを計算
							Vector3 localDirection = physBoneCollider.rotation * Vector3.up; // Y軸方向

							// 終点位置を計算
							Vector3 localStartPosition = physBoneCollider.position - localDirection * (physBoneCollider.height / 4);
							Vector3 localEndPosition = physBoneCollider.position + localDirection * (physBoneCollider.height / 4);

							collider.Tail = localEndPosition;
							collider.Offset = localStartPosition;

							// 作ったので追加する
							EditorUtility.SetDirty(collider);
							createdColliderGroup.Colliders.Add(collider);
						}

						// 作ったコライダーグループを追加
						EditorUtility.SetDirty(createdColliderGroup);
						bones.ColliderGroups.Add(createdColliderGroup);
						currentColliders.Add(u.Item1, new List<VRM10SpringBoneColliderGroup>() { createdColliderGroup });
					}

					// 階層が一致するSpringJointを見つける
					var spring = bones.Springs.First(b => AvatarFixerUtility.GetTransformPath(rootBone, b.Joints.First().transform) == u.Item2);

					// Springに追加
					spring.ColliderGroups.AddRange(currentColliders[u.Item1]);
				}

			}
			EditorUtility.SetDirty(vrmInstance);

		}

		/// <summary>
		/// SpringJointの変換
		/// </summary>
		/// <param name="spring">修正対象</param>
		/// <param name="source">XWearの元</param>
		private static void ConvertSpringJoint(Vrm10InstanceSpringBone.Spring spring, AccessoryRootComponent source)
		{
			// Jointは必ず親が1つ足りないので追加させる
			AddParentJointForVrmSpringBone(spring);

			// 一致するPhysBoneを探す
			// ヒエラルキーの階層構造が一致しているかで判断する仕組み
			var sourcePhysBones = source.GetComponentsInChildren<VRCPhysBone>();
			VRCPhysBone sourcePhysBone = GetSourcePhysBone(sourcePhysBones, spring);

			// 値を修正する
			FixSpringJoint(sourcePhysBone, spring);
		}

		/// <summary>
		/// SpringJointの変換
		/// </summary>
		/// <param name="spring">修正対象</param>
		/// <param name="source">XWearの元</param>
		private static void ConvertSpringJoint(Humanoid humanoid, Vrm10InstanceSpringBone.Spring spring, HumanoidMapComponent source)
		{
			// Jointは必ず親が1つ足りないので追加させる
			AddParentJointForVrmSpringBone(spring);

			// 一致するPhysBoneを探す
			// ヒエラルキーの階層構造が一致しているかで判断する仕組み

			// 頭のジョイントからたどる
			Transform currentJoint = spring.Joints.First().transform;
			Transform preJoint = null;
			HumanBodyBones ? activeBoneName = null;
			// ドンドン親をチェックしてそれが規定のボーンかどうかを見る
			while (activeBoneName == null)
			{
				preJoint = currentJoint;
				currentJoint = currentJoint.parent;
				var bonemaps = humanoid.BoneMap.Where(bm => bm.Item1 == currentJoint);
				if (bonemaps.Count() == 0) continue;
				activeBoneName = bonemaps.First().Item2;
			}

			// 一致するPhysBoneを探す
			// ヒエラルキーの階層構造が一致しているかで判断する仕組み
			var matchedBone = source.HumanoidMap.GetMap.FindFirstKeyByValue(activeBoneName.Value);
			var sourcePhysBones = matchedBone.GetComponentsInChildren<VRCPhysBone>();
			VRCPhysBone sourcePhysBone = GetSourcePhysBone(sourcePhysBones, spring);

			// 値を修正する
			FixSpringJoint(sourcePhysBone, spring);
		}

		/// <summary>
		/// 指定されたSpringJoint群と一致する階層パターンのPhysBoneを探し出して返す
		/// 
		/// </summary>
		/// <param name="sourcePhysBones">探す対象のPhysBone一覧</param>
		/// <param name="spring">比較対象のSpring</param>
		/// <returns>見つけたPhysBone</returns>
		private static VRCPhysBone GetSourcePhysBone(VRCPhysBone[] sourcePhysBones, Vrm10InstanceSpringBone.Spring spring)
		{
			var springPath = AvatarFixerUtility.GetTransformPath(spring.Joints.First().transform, spring.Joints.Last().transform);
			VRCPhysBone sourcePhysBone = null;
			foreach (var sp in sourcePhysBones)
			{
				List<Transform> deepTransforms = new List<Transform>();
				var rootTransform = sp.GetRootTransform();
				AvatarFixerUtility.GetDeepTransform(rootTransform, deepTransforms);
				var deepPath = deepTransforms.Select(t => AvatarFixerUtility.GetTransformPath(rootTransform, t));
				if (!deepPath.Contains(springPath)) continue;
				sourcePhysBone = sp;
				break;
			}
			if (sourcePhysBone == null)
			{
				Debug.LogErrorFormat("一致するPhysBoneが見つかりません: {0}", springPath);
			}
			return sourcePhysBone;
		}

		/// <summary>
		/// SpringJointの値をPhysBoneの値に上書きする
		/// </summary>
		/// <param name="sourcePhysBone">目安となるPhysBone</param>
		/// <param name="spring">修正対象のSpringJoint群</param>
		public static void FixSpringJoint(VRCPhysBone sourcePhysBone, Vrm10InstanceSpringBone.Spring spring)
		{
			// PhysBoneとSprintJointを紐づける
			List<Tuple<VRM10SpringBoneJoint, Transform>> springJointWithPhysJoint = new List<Tuple<VRM10SpringBoneJoint, Transform>>();
			Transform activePhysJoint = sourcePhysBone.GetRootTransform();
			activePhysJoint = activePhysJoint.parent;
			foreach (var j in spring.Joints)
			{
				activePhysJoint = activePhysJoint.Find(j.name);
				springJointWithPhysJoint.Add(new Tuple<VRM10SpringBoneJoint, Transform>(j, activePhysJoint));
			}

			// EndPointが0以外ならオブジェクトが必要と判断
			if (sourcePhysBone.endpointPosition != Vector3.zero)
			{
				// 末端にEndJointがないかチェックして生成
				var instance = GameObject.Instantiate(springJointWithPhysJoint.Last().Item1);
				instance.transform.SetParent(springJointWithPhysJoint.Last().Item1.transform);
				instance.name = springJointWithPhysJoint.Last().Item1.name + "_end";
				instance.transform.localPosition = sourcePhysBone.endpointPosition;
				Undo.RegisterCreatedObjectUndo(instance.gameObject, "CreateObject");
				springJointWithPhysJoint.Add(new Tuple<VRM10SpringBoneJoint, Transform>(instance, null));
				spring.Joints.Add(instance);
			}

			for (int i = 0; i < springJointWithPhysJoint.Count; i++)
			{
				Undo.RecordObject(springJointWithPhysJoint[i].Item1, "RefreshValue");
				float percentage = (float)i / springJointWithPhysJoint.Count;

				// 下記を参考に推測で組み込み
				// 大体同じ動きに見えたのでとりあえずOK
				// https://creators.vrchat.com/avatars/avatar-dynamics/physbones/
				// https://wiki.virtualcast.jp/wiki/vrm/setting/spring
				springJointWithPhysJoint[i].Item1.m_jointRadius = sourcePhysBone.CalcRadius(percentage);
				springJointWithPhysJoint[i].Item1.m_gravityDir = new Vector3(0, -Mathf.Sign(sourcePhysBone.gravity), 0);
				springJointWithPhysJoint[i].Item1.m_gravityPower = sourcePhysBone.CalcGravity(percentage);
				if (sourcePhysBone.integrationType == VRC.Dynamics.VRCPhysBoneBase.IntegrationType.Simplified)
				{
					springJointWithPhysJoint[i].Item1.m_dragForce = sourcePhysBone.CalcSpring(percentage);
					springJointWithPhysJoint[i].Item1.m_stiffnessForce = sourcePhysBone.CalcPull(percentage);
				}
				else
				{
					// TODO: Momentumが非公開APIなので一旦そのまま
					springJointWithPhysJoint[i].Item1.m_dragForce = sourcePhysBone.CalcSpring(percentage);
					springJointWithPhysJoint[i].Item1.m_stiffnessForce = sourcePhysBone.CalcStiffness(percentage);
				}
				EditorUtility.SetDirty(springJointWithPhysJoint[i].Item1);
			}
		}

		/// <summary>
		/// 頭のJointの親にJointを追加
		/// </summary>
		/// <param name="spring">追加対象</param>
		private static void AddParentJointForVrmSpringBone(Vrm10InstanceSpringBone.Spring spring)
		{
			// Jointは必ず親が1つ足りないので追加させる
			var firstJoint = spring.Joints.First();
			var parentTarget = firstJoint.transform.parent;
			var jointComp = Undo.AddComponent<VRM10SpringBoneJoint>(parentTarget.gameObject);
			spring.Joints.Insert(0, jointComp);
		}

		/// <summary>
		/// 該当衣装がVRMに使用中かをチェック
		/// 破壊的に組み込まれるため、抽象的な一致で確認するしかない
		/// </summary>
		/// <param name="vrmInstance">VRM</param>
		/// <param name="wear">衣装</param>
		/// <returns>使用中の衣装である</returns>
		private static bool IsUsingHumanoidMapWear(VRM10SpringBoneJoint rootJoint, Vrm10Instance vrmInstance, XWearData wear)
		{
			// 元プレハブにHumanoidMapがあればまずこれは衣装
			if (!wear.isHumanoidMap) return false;

			// 衣装のメッシュとVRMのメッシュを比較
			foreach (var wsmr in wear.wear.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				var wmesh = wsmr.sharedMesh;
				// 衣装のSkinnedMeshRendererの中身を1つずつチェック
				var smr = vrmInstance.GetComponentsInChildren<SkinnedMeshRenderer>()
					// 大体は親直下にあると思われる
					.Where(r => r.transform.parent == vrmInstance.transform)
					// メッシュの中身や名前で一致チェック
					.Where(r => {
						if (r.name != wsmr.name) return false;
						var mesh = r.sharedMesh;
						if (mesh.name != $"{wmesh.name}.baked") return false;
						if (mesh.vertexCount != wmesh.vertexCount) return false;
						if (mesh.subMeshCount != wmesh.subMeshCount) return false;
						if (!r.bones.Contains(rootJoint.transform)) return false;
						return true;
					})
					.FirstOrDefault();
				// 一つも見つからないなら違っていた
				if (smr == null) continue;
				// 見つかったなら間違いない
				return true;
			}
			// 見つからなかった
			return false;
		}

		/// <summary>
		/// 該当アクセサリーがVRMに使用中かをチェック
		/// ボーンに埋め込まれる形で組まれているので、ツリーをたどって名前でチェックする
		/// </summary>
		/// <param name="rootJoint"></param>
		/// <param name="wear"></param>
		/// <returns></returns>
		private static bool IsUsingAccessoryRootComponentWear(VRM10SpringBoneJoint rootJoint, XWearData wear)
		{
			string wearName = wear.name;
			var nowTransform = rootJoint.transform;
			List<Transform> boneCaches = new List<Transform>();
			bool matchName = false;
			while (nowTransform != null && !matchName)
			{
				boneCaches.Add(nowTransform);
				nowTransform = nowTransform.parent;
				matchName = nowTransform != null && nowTransform.name == wearName;
			}
			if (matchName) return true;
			return false;
		}

		/// <summary>
		/// 使っていないSpringBoneを消去する
		/// </summary>
		/// <param name="vrmInstance">対象VRM</param>
		public static void RemoveUnuseSpringBone(Vrm10Instance vrmInstance)
		{
			Undo.RecordObject(vrmInstance, "Remove Spring Bone");

			// 現状の使用中コライダーグループを列挙
			var beforeColliders = vrmInstance.SpringBone.Springs
				.SelectMany(s => s.ColliderGroups)
				.Where(s => s != null)
				.ToList();

			// Joint数がゼロやnullしかないSpringは消去する
			vrmInstance.SpringBone.Springs = vrmInstance.SpringBone.Springs
				.Where(s => !(s.Joints.All(null) || s.Joints.Count == 0))
				.ToList();

			// 改めて現状のコライダーグループをチェック
			var afterColliders = vrmInstance.SpringBone.Springs
				.SelectMany(s => s.ColliderGroups);

			// 前のコライダーグループの一覧と比較して消去対象を選別
			var removedColliders = beforeColliders
				// 新しいほうに存在しなければ「不要になった」と判断
				.Where(c => !afterColliders.Contains(c));

			// コライダーグループの一覧を整理
			vrmInstance.SpringBone.ColliderGroups = vrmInstance.SpringBone.ColliderGroups
				// 空のものは消去
				.Where(c => c != null)
				// 削除対象リストに入っているものを消去
				.Where(c => !removedColliders.Contains(c))
				.ToList();

			// 未参照のコライダーグループを抽出する
			var unusingColliderGroups = vrmInstance.GetComponentsInChildren<VRM10SpringBoneColliderGroup>()
				.Where(c => !vrmInstance.SpringBone.ColliderGroups.Contains(c));

			// コライダーグループを消去する
			foreach (var c in unusingColliderGroups)
			{
				Undo.DestroyObjectImmediate(c);
			}

			// 現状残っているコライダーグループを抽出
			var activeColliders = vrmInstance.GetComponentsInChildren<VRM10SpringBoneColliderGroup>()
				.SelectMany(c => c.Colliders);

			// 未参照のコライダーを抽出
			var unusedColliders = vrmInstance.GetComponentsInChildren<VRM10SpringBoneCollider>()
				.Where(c => !activeColliders.Contains(c));

			// コライダーを削除する
			foreach (var c in unusedColliders)
			{
				Undo.DestroyObjectImmediate(c);
			}

			// Jointのnullを消去する
			foreach (var s in vrmInstance.SpringBone.Springs)
			{
				s.Joints = s.Joints.Where(j => j != null).ToList();
			}

			// 反映
			EditorUtility.SetDirty(vrmInstance);
		}

	}

}

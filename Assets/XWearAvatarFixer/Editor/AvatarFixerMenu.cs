using System.Linq;
using UnityEditor;
using UnityEngine;
using UniVRM10;
using System.IO;

namespace pspkurara.VRM10FromXRoidAvatarFixer.Editor
{
	using Runtime;

	/// <summary>
	/// アバター変換のメニュー類
	/// </summary>
	static class AvatarFixerMenu
	{

		const string LastExportPathKey = "AVATAR FIXER LAST EXPORT PATH";

		const string AssetContext = "Assets/";
		const string GameObjectContext = "GameObject/";
		const string DialogTitleContext = "Avatar Fixer";
		const string MenuItemContextHeader = "XWear Avatar Fixer/";
		const string CreateXWearSourceMenuItemContext = "Create/" + MenuItemContextHeader + "XWearSource";
		const string FromXRoidAvatarFixingContext = MenuItemContextHeader + "From XRoid Avatar Fixing";
		const string RemoveUnusedSpringBonesCollidersTransformContext = MenuItemContextHeader + "Remove Unused Spring Bones, Colliders, Transforms";
		const string Optimization = MenuItemContextHeader + "Optimization";

		/// <summary>
		/// XRoidから書き出したVRM1.0ファイルを修復して書き出す
		/// </summary>
		[MenuItem(AssetContext + FromXRoidAvatarFixingContext)]
		public static void FixAndExportAvatar()
		{

			// 複数まとめて変換可能
			var selectingAvatarRoot = Selection.objects
				.Where(o => o != null);

			// 1つも選んでいなかったらやめる
			if (selectingAvatarRoot.Count() == 0) return;

			// XWearの元データがないとだめ
			if (CheckNotExsitsXWearSourceDialog()) return;

			// エクスポート先を選ぶ
			// 選んだフォルダにまとめて出力されるので注意
			var path = GetDir("Export Directory");
			if (string.IsNullOrEmpty(path)) return;

			Debug.Log("Start Covert.");

			try
			{

				float count = 1f / selectingAvatarRoot.Count();
				float currentCount = 0;
				foreach (var o in selectingAvatarRoot)
				{
					// 1つずつエクスポートする
					if (EditorUtility.DisplayCancelableProgressBar("Exporting...", o.name, currentCount))
					{
						return;
					}
					Vrm10Export export = new Vrm10Export(o as GameObject);
					// 保存前に修復処理を行う
					export.ExportPreLogic = (go) =>
					{
						// 修復処理
						AvatarFixer.ConvertSpringBone(go.GetComponent<Vrm10Instance>());
					};
					// 書き出し
					export.Export(Path.Combine(path, $"{o.name}.vrm"));
					Debug.Log($"Completed {o.name}");
				}
				Debug.Log("Convert Completed.");
				// 書き出し終えたのでダイアログを出してやる
				EditorUtility.RevealInFinder(path);

			}
			catch(System.Exception e)
			{
				// 失敗したとき用に例外処理
				Debug.LogException(e);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		/// <summary>
		/// XRoidから書き出したVRM1.0プレハブを修復する
		/// ヒエラルキー上にインスタンスされている状態でなければ動作しない
		/// 形式がVRM1.0と同じであれば編集中のプレハブでも動作する筈
		/// </summary>
		[MenuItem(GameObjectContext + FromXRoidAvatarFixingContext)]
		public static void FixAvatar()
		{
			var selectingAvatarRoot = Selection.objects
				.Cast<GameObject>()
				.Select(o => o.GetComponent<Vrm10Instance>())
				.Where(o => o != null);

			// 1つも選んでいなかったらやめる
			if (selectingAvatarRoot.Count() == 0) return;

			// XWearの元データがないとだめ
			if (CheckNotExsitsXWearSourceDialog()) return;

			Debug.Log("Start Covert.");
			foreach (var o in selectingAvatarRoot)
			{
				// 1つずつ変換する
				AvatarFixer.ConvertSpringBone(o);
				Debug.Log($"Completed {o.name}");
			}

			Debug.Log("Convert Completed.");
		}

		/// <summary>
		/// VRM1.0プレハブの使っていないSpringJointとCollider、使っていないTransformを消去する
		/// ヒエラルキー上にインスタンスされている状態でなければ動作しない
		/// 形式がVRM1.0と同じであれば編集中のプレハブでも動作する筈
		/// </summary>
		[MenuItem(GameObjectContext + RemoveUnusedSpringBonesCollidersTransformContext)]
		public static void RemoveUnusedSpringBoneCollidersTransforms()
		{
			var selectingAvatarRoot = Selection.objects
				.Cast<GameObject>()
				.Select(o => o.GetComponent<Vrm10Instance>())
				.Where(o => o != null);

			// 1つも選んでいなかったらやめる
			if (selectingAvatarRoot.Count() == 0) return;

			Debug.Log("Start Covert.");
			foreach (var o in selectingAvatarRoot)
			{
				// 1つずつ変換する
				AvatarFixer.RemoveUnuseSpringBone(o);
				Debug.Log($"Completed {o.name}");
			}

			Debug.Log("Convert Completed.");
		}

		/// <summary>
		/// XRoidから書き出したVRM1.0ファイルを修復して書き出す
		/// その他の全修正を挟む
		/// </summary>
		[MenuItem(AssetContext + Optimization)]
		public static void OptimizationAndExportAvatar()
		{
			// 複数まとめて変換可能
			var selectingAvatarRoot = Selection.objects
				.Where(o => o != null);

			// 1つも選んでいなかったらやめる
			if (selectingAvatarRoot.Count() == 0) return;

			// XWearの元データがないとだめ
			if (CheckNotExsitsXWearSourceDialog()) return;

			// エクスポート先を選ぶ
			// 選んだフォルダにまとめて出力されるので注意
			var path = GetDir("Export Directory");
			if (string.IsNullOrEmpty(path)) return;

			Debug.Log("Start Optimize.");

			try
			{

				float count = 1f / selectingAvatarRoot.Count();
				float currentCount = 0;
				foreach (var o in selectingAvatarRoot)
				{
					// 1つずつエクスポートする
					if (EditorUtility.DisplayCancelableProgressBar("Exporting...", o.name, currentCount))
					{
						return;
					}
					Vrm10Export export = new Vrm10Export(o as GameObject);
					// 保存前に修復処理を行う
					export.ExportPreLogic = (go) =>
					{
						// 修復処理
						var vrmInstance = go.GetComponent<Vrm10Instance>();
						AvatarFixer.ConvertSpringBone(vrmInstance);
						// 最適化処理
						AvatarFixer.RemoveUnuseSpringBone(vrmInstance);
					};
					// 書き出し
					export.Export(Path.Combine(path, $"{o.name}.vrm"));
					Debug.Log($"Completed {o.name}");
				}
				Debug.Log("Optimize Completed.");
				// 書き出し終えたのでダイアログを出してやる
				EditorUtility.RevealInFinder(path);

			}
			catch (System.Exception e)
			{
				// 失敗したとき用に例外処理
				Debug.LogException(e);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		/// <summary>
		/// XRoidから書き出したVRM1.0プレハブを修復する
		/// その他の全修正を挟む
		/// ヒエラルキー上にインスタンスされている状態でなければ動作しない
		/// 形式がVRM1.0と同じであれば編集中のプレハブでも動作する筈
		/// </summary>
		[MenuItem(GameObjectContext + Optimization)]
		public static void OptimizationAvatar()
		{

			var selectingAvatarRoot = Selection.objects
				.Cast<GameObject>()
				.Select(o => o.GetComponent<Vrm10Instance>())
				.Where(o => o != null);

			// 1つも選んでいなかったらやめる
			if (selectingAvatarRoot.Count() == 0) return;

			// XWearの元データがないとだめ
			if (CheckNotExsitsXWearSourceDialog()) return;

			Debug.Log("Start Optimize.");
			foreach (var o in selectingAvatarRoot)
			{
				// 1つずつ変換する
				AvatarFixer.ConvertSpringBone(o);
				AvatarFixer.RemoveUnuseSpringBone(o);
				Debug.Log($"Completed {o.name}");
			}

			Debug.Log("Optimize Completed.");
		}

		/// <summary>
		/// XWearSource.assetを生成する
		/// </summary>
		[MenuItem(AssetContext + CreateXWearSourceMenuItemContext)]
		public static void CreateMyScriptableObject()
		{
			// 既に存在する場合は警告を出しておく
			// Yesを押せば新しく作れるようにもしておく
			if (AvatarFixer.IsExsitsXWearSource() && !EditorUtility.DisplayDialog(DialogTitleContext,
				$"Already exists {typeof(XWearSource).Name}.asset. create new one?\nOnly the first one found will be referenced.", "Yes", "Cancel"))
			{
				return;
			}

			// ScriptableObjectのインスタンスを作成
			XWearSource instance = ScriptableObject.CreateInstance<XWearSource>();

			// アセットとして保存するためのパスを指定
			// 選択しているオブジェクトの隣に作る
			string finalPath = "Assets";
			if (Selection.activeObject != null)
			{
				finalPath = AssetDatabase.GetAssetPath(Selection.activeObject);
				if (Path.HasExtension(finalPath))
				{
					finalPath = Path.GetDirectoryName(finalPath);
				}
			}
            string path = AssetDatabase.GenerateUniqueAssetPath($"{finalPath}/{typeof(XWearSource).Name}.asset");

			// アセットとして保存
			AssetDatabase.CreateAsset(instance, path);
			AssetDatabase.SaveAssets();

			// 作成したアセットを選択
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = instance;
		}


		/// <summary>
		/// ダイアログを立ち上げて返す
		/// キャンセルでnullか空文字を返す
		/// </summary>
		/// <param name="title">ダイアログタイトル</param>
		/// <param name="dir">初期ディレクトリ</param>
		/// <returns>決定したパス</returns>
		private static string GetDir(string title, string dir = null)
		{
			// UniGLTF.SaveFileDialog.GetDirと中身は殆ど同じ
			string directory = string.IsNullOrEmpty(dir) ? EditorPrefs.GetString(LastExportPathKey) : dir;
			if (string.IsNullOrEmpty(directory))
			{
				directory = Directory.GetParent(Application.dataPath).ToString();
			}

			var path = EditorUtility.SaveFolderPanel(title, directory, null);
			if (!string.IsNullOrEmpty(path))
			{
				EditorPrefs.SetString(LastExportPathKey, Path.GetDirectoryName(path).Replace("\\", "/"));
			}

			return path;
		}

		/// <summary>
		/// XWearSourceが存在するかを確認し、なければダイアログを出す
		/// </summary>
		/// <returns>存在しない</returns>
		private static bool CheckNotExsitsXWearSourceDialog()
		{
			// 確認する
			if(!AvatarFixer.IsExsitsXWearSource())
			{
				EditorUtility.DisplayDialog(DialogTitleContext,
				$"Please Create {typeof(XWearSource)}.asset on any Assets/ directory.\ncheck {CreateXWearSourceMenuItemContext}.", "Close");
				return true;
			}
			return false;
		}


	}
}

using System.Collections;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Windows;
public class MUADCLProject : MonoBehaviour
{
    public string rootFolder_;  // Backing store
    // Start is called before the first frame update
    public string rootFolder {
        get
        {
            return rootFolder_;
        }
        set
        {
            rootFolder_ = value;
            //Load();
        }
    }
    public UnityEngine.UI.InputField input;
    void Start()
    {
    }
    void Update()
    {
    }
    public void Load()
    {
        rootFolder_ = input.text;
        DoLoad();
    }
    // Load all glb files.
    public async Task DoLoad()
    {
        RLD.RTPrefabLibDb.Get.Clear();
        var modelPath = rootFolder_ + "/models/";
        var LibSingleton = RLD.RTPrefabLibDb.Get;
        var lib = LibSingleton.CreateLib(modelPath);
        LibSingleton.EditorPrefabPreviewGen.BeginGenSession(LibSingleton.PrefabPreviewLookAndFeel);
        foreach (var file in System.IO.Directory.EnumerateFiles(modelPath, "*.glb"))
        {
            var go = new GameObject();
            go.name = file.Substring(modelPath.Length);
            var gltf = go.AddComponent<GLTFast.GltfAsset>();
            await gltf.Load(file);
            var editorComponent = go.AddComponent<MUADCLObject>();
            editorComponent.GLTFRelativePath = "models/" + file.Substring(modelPath.Length);
            var texture = LibSingleton.EditorPrefabPreviewGen.Generate(go);
            lib.CreatePrefab(go, texture);
            go.SetActive(false);
        }
        LibSingleton.EditorPrefabPreviewGen.EndGenSession();
    }
    public void Export()
    {
        DoExport();
    }
    public Dictionary<string, int> usedModels;
    public int entityIndex, shapeIndex;
    public void CreateShapeRecursively(GameObject go, StreamWriter file, int muaParentIndex)
    {
        if (!go.active)
            return;
        var shape = go.GetComponent<MUADCLObject>();

        if (shape)
        {
            if (!usedModels.ContainsKey(shape.GLTFRelativePath))
            {
                file.WriteLine($"const shape_{shapeIndex} = new GLTFShape('{shape.GLTFRelativePath}');");
                usedModels.Add(shape.GLTFRelativePath, shapeIndex++);
            }
            int currentShapeIndex;
            usedModels.TryGetValue(shape.GLTFRelativePath, out currentShapeIndex);
            file.WriteLine($"const entity_{entityIndex} = new Entity();");
            file.WriteLine($"entity_{entityIndex}.addComponent(new Transform({{");
            file.WriteLine($"position:new Vector3({go.transform.localPosition.x},{go.transform.localPosition.y},{go.transform.localPosition.z}),");
            file.WriteLine($"rotation:new Quaternion({go.transform.localRotation.x},{go.transform.localRotation.y},{go.transform.localRotation.z},{go.transform.localRotation.w}),");
            file.WriteLine($"scale:new Vector3({go.transform.localScale.x},{go.transform.localScale.y},{go.transform.localScale.z})");
            file.WriteLine($"}}));");
            file.WriteLine($"entity_{entityIndex}.addComponent(shape_{currentShapeIndex});");
            if (muaParentIndex != -1)
                file.WriteLine($"entity_{entityIndex}.setParent(entity_{muaParentIndex});");
            file.WriteLine($"engine.addEntity(entity_{entityIndex});");
            muaParentIndex = entityIndex;
            entityIndex++;
        }

        for (int i = 0; i < go.transform.childCount;++i)
        {
            var child = go.transform.GetChild(i);
            CreateShapeRecursively(child.gameObject, file, muaParentIndex);
        }
    }
    public async Task DoExport()
    {
        using (StreamWriter file = new StreamWriter(rootFolder_ + "/src/game.ts"))
        {
            file.WriteLine("import * as utils from '@dcl/ecs-scene-utils'");
            entityIndex = 0;
            shapeIndex = 0;
            usedModels = new Dictionary<string, int>();
            foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                CreateShapeRecursively(go, file, -1);
        }
    }
}

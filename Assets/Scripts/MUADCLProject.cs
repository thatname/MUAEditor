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
    public async Task DoExport()
    {
        using (StreamWriter file = new StreamWriter(rootFolder_ + "/src/game.ts"))
        {
            file.WriteLine("import * as utils from '@dcl/ecs-scene-utils'");
            int entityIndex = 0;
            int shapeIndex = 0;
            var usedModels = new Dictionary<string, int>();
            foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if(go.active)
                {
                    // TODO: Create entity hierarchy recursively.
                    foreach (var shape in go.GetComponentsInChildren<MUADCLObject>())
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
                            file.WriteLine($"position:new Vector3({go.transform.position.x},{go.transform.position.y},{go.transform.position.z}),");
                            file.WriteLine($"rotation:new Quaternion({go.transform.rotation.x},{go.transform.rotation.y},{go.transform.rotation.z},{go.transform.rotation.w}),");
                            file.WriteLine($"scale:new Vector3({go.transform.localScale.x},{go.transform.localScale.y},{go.transform.localScale.z})");
                            file.WriteLine($"}}));");
                            file.WriteLine($"entity_{entityIndex}.addComponent(shape_{currentShapeIndex});");
                            file.WriteLine($"engine.addEntity(entity_{entityIndex});");
                            entityIndex++;
                        }
                }
        }
    }
}

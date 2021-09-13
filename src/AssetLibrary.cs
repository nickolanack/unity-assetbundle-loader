using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AssetLibrary : MonoBehaviour
{

    public static AssetLibrary Library;
    public string url;


    Dictionary<string, AssetBundle> lib=new Dictionary<string, AssetBundle>();

    public delegate void BundleEvent(AssetBundle bundle);
    Dictionary<string, List<BundleEvent>> onBundleLoadedListeners= new Dictionary<string, List<BundleEvent>>();
    List<string> bundleLoading=new List<string>();

    public delegate void AssetEvent(GameObject asset);

    public delegate void TextureEvent(Texture2D texture);
    Dictionary<string, List<TextureEvent>> onTextureLoadedListeners= new Dictionary<string, List<TextureEvent>>();
    List<string> textureLoading=new List<string>();


    public delegate void AssetListCallback(string[] list);

    Queue<string> bundleQueue=new Queue<string>();
    Queue<string> textureQueue=new Queue<string>();


    void Start()
    {
        AssetLibrary.Library=this;
        RequireBundle("cube");

    }

    // Update is called once per frame
    void Update()
    {
        if(bundleQueue.Count>0){
            StartCoroutine(GetAssetBundle(bundleQueue.Dequeue()));
            return;
        }


        if(textureQueue.Count>0){
            StartCoroutine(GetTexture(textureQueue.Dequeue()));
            return;
        }

    }

    public void RequireBundle(string bundle){
        if(lib.ContainsKey(bundle)){
            return;
        }
        if(bundleLoading.Contains(bundle)){
            return;
        }
        bundleLoading.Add(bundle);
        onBundleLoadedListeners.Add(bundle, new List<BundleEvent>());
        bundleQueue.Enqueue(bundle);
    }

    public void GetBundle(string bundle, BundleEvent callback){

        if(lib.ContainsKey(bundle)){

            callback(lib[bundle]);
            return;
        }

        if(bundleLoading.Contains(bundle)){
            onBundleLoadedListeners[bundle].Add(callback);
            return;
        }

        RequireBundle(bundle);

    }


    public void ListAssets(string bundle, AssetListCallback callback){
        GetBundle(bundle, delegate(AssetBundle assetBundle){
            callback(assetBundle.GetAllAssetNames());
        });
    }


    public void LoadAsset(string bundle, string asset, AssetEvent callback){

        if(lib.ContainsKey(bundle)){

            GameObject obj = (GameObject)lib[bundle].LoadAsset(asset);
            callback(obj);

            return;
        }

        if(bundleLoading.Contains(bundle)){

            onBundleLoadedListeners[bundle].Add(delegate(AssetBundle b){
                LoadAsset(bundle, asset, callback);
            });

            return;
        }

        RequireBundle(bundle);

    }


    IEnumerator GetAssetBundle(string bundleName) {
       
        UnityWebRequest assetBundleRequest = UnityWebRequestAssetBundle.GetAssetBundle(url+"/"+bundleName);
        yield return assetBundleRequest.SendWebRequest();
 
        if (assetBundleRequest.result != UnityWebRequest.Result.Success) {
            Debug.Log(assetBundleRequest.error);
        }
        else {

            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(assetBundleRequest);
            
            lib.Add(bundleName, bundle);
            bundleLoading.Remove(bundleName);


            foreach(string assetName in bundle.GetAllAssetNames()){
                Debug.Log("Loaded AssetBundle("+bundleName+"): "+assetName);
            }

            foreach(BundleEvent listener in onBundleLoadedListeners[bundleName]){
                listener(bundle);
            }
            onBundleLoadedListeners.Remove(bundleName);
        }
    }


    public void LoadTexture(string textureName, TextureEvent callback){





        if(!textureLoading.Contains(textureName)){
            onTextureLoadedListeners.Add(textureName, new List<TextureEvent>());
            textureLoading.Add(textureName);
            textureQueue.Enqueue(textureName);
        }

        onTextureLoadedListeners[textureName].Add(callback);

       
    

        

    }

    IEnumerator GetTexture(string textureName) {
        UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(url+"/"+textureName);
        yield return textureRequest.SendWebRequest();


        if (textureRequest.result != UnityWebRequest.Result.Success) {
            Debug.Log(textureRequest.error);
        }
        else {

            Texture2D texture = (Texture2D) DownloadHandlerTexture.GetContent(textureRequest);

            textureLoading.Remove(textureName);
            foreach(TextureEvent listener in onTextureLoadedListeners[textureName]){
                listener(texture);
            }
            onTextureLoadedListeners.Remove(textureName);

        }
    }







}

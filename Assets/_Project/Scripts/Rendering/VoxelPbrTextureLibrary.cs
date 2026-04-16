using UnityEngine;

namespace WorldOfVictoria.Rendering
{
    [CreateAssetMenu(fileName = "VoxelPbrTextureLibrary", menuName = "World of Victoria/Rendering/Voxel PBR Texture Library")]
    public sealed class VoxelPbrTextureLibrary : ScriptableObject
    {
        [Header("Materials")]
        [SerializeField] private Material stonePbr;
        [SerializeField] private Material grassPbr;

        [Header("Stone")]
        [SerializeField] private Texture2D stoneAlbedo;
        [SerializeField] private Texture2D stoneNormal;
        [SerializeField] private Texture2D stoneHeight;
        [SerializeField] private Texture2D stoneAo;
        [SerializeField] private Texture2D stoneRoughness;
        [SerializeField] private Texture2D stoneMetallic;

        [Header("Grass Top")]
        [SerializeField] private Texture2D grassTopAlbedo;
        [SerializeField] private Texture2D grassTopNormal;
        [SerializeField] private Texture2D grassTopHeight;
        [SerializeField] private Texture2D grassTopAo;
        [SerializeField] private Texture2D grassTopRoughness;
        [SerializeField] private Texture2D grassTopMetallic;

        [Header("Grass Side")]
        [SerializeField] private Texture2D grassSideAlbedo;
        [SerializeField] private Texture2D grassSideNormal;
        [SerializeField] private Texture2D grassSideHeight;
        [SerializeField] private Texture2D grassSideAo;
        [SerializeField] private Texture2D grassSideRoughness;
        [SerializeField] private Texture2D grassSideMetallic;

        [Header("Grass Bottom")]
        [SerializeField] private Texture2D grassBottomAlbedo;
        [SerializeField] private Texture2D grassBottomNormal;
        [SerializeField] private Texture2D grassBottomHeight;
        [SerializeField] private Texture2D grassBottomAo;
        [SerializeField] private Texture2D grassBottomRoughness;
        [SerializeField] private Texture2D grassBottomMetallic;

        [Header("Texture Arrays")]
        [SerializeField] private Texture2DArray albedoArray;
        [SerializeField] private Texture2DArray normalArray;
        [SerializeField] private Texture2DArray roughnessArray;

        public Material StonePbr => stonePbr;
        public Material GrassPbr => grassPbr;
        public Texture2D StoneAlbedo => stoneAlbedo;
        public Texture2D StoneNormal => stoneNormal;
        public Texture2D StoneHeight => stoneHeight;
        public Texture2D StoneAo => stoneAo;
        public Texture2D StoneRoughness => stoneRoughness;
        public Texture2D StoneMetallic => stoneMetallic;
        public Texture2D GrassTopAlbedo => grassTopAlbedo;
        public Texture2D GrassTopNormal => grassTopNormal;
        public Texture2D GrassTopHeight => grassTopHeight;
        public Texture2D GrassTopAo => grassTopAo;
        public Texture2D GrassTopRoughness => grassTopRoughness;
        public Texture2D GrassTopMetallic => grassTopMetallic;
        public Texture2D GrassSideAlbedo => grassSideAlbedo;
        public Texture2D GrassSideNormal => grassSideNormal;
        public Texture2D GrassSideHeight => grassSideHeight;
        public Texture2D GrassSideAo => grassSideAo;
        public Texture2D GrassSideRoughness => grassSideRoughness;
        public Texture2D GrassSideMetallic => grassSideMetallic;
        public Texture2D GrassBottomAlbedo => grassBottomAlbedo;
        public Texture2D GrassBottomNormal => grassBottomNormal;
        public Texture2D GrassBottomHeight => grassBottomHeight;
        public Texture2D GrassBottomAo => grassBottomAo;
        public Texture2D GrassBottomRoughness => grassBottomRoughness;
        public Texture2D GrassBottomMetallic => grassBottomMetallic;
        public Texture2DArray AlbedoArray => albedoArray;
        public Texture2DArray NormalArray => normalArray;
        public Texture2DArray RoughnessArray => roughnessArray;
    }
}

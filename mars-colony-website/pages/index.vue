<template>
  <div>
    <canvas
      ref="unityCanvas"
      width="1280"
      height="800"
      style="width: 1280px; height: 800px; background: #121212"
    ></canvas>
  </div>
</template>

<script>
export default {
  data: () => ({
    unity: null,
  }),
  async mounted() {
    window.load = this.load;
    window.save = this.save;
    
    this.$refs.unityCanvas.width = window.innerWidth;
    this.$refs.unityCanvas.height = window.innerHeight;
    this.$refs.unityCanvas.style.width = `${window.innerWidth}px`;
    this.$refs.unityCanvas.style.height = `${window.innerHeight}px`;
    
    this.unity = await createUnityInstance(this.$refs.unityCanvas, {
      dataUrl: '/build.data',
      frameworkUrl: '/build.framework.js',
      codeUrl: '/build.wasm',
      streamingAssetsUrl: 'StreamingAssets"',
      companyName: 'Wesley Matthews',
      productName: 'Mars Colony',
      productVersion: '1.0',
    });
    
    this.$store.commit('loadFromLocalStorage');
    
    if (this.$store.state.saveData) {
      this.unity.SendMessage('Server', 'EnableContinueButton');
    }
  },
  methods: {
    load() {
      this.unity.SendMessage('Server', 'ReceiveGameSave', this.$store.state.saveData);
    },
    save(saveData) {
      this.$store.commit('save', saveData);
      this.unity.SendMessage('Server', 'SavedSuccessfully');
    },
  },
}
</script>

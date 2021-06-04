export const state = () => ({
  saveData: null,
});

export const mutations = {
  loadFromLocalStorage(state) {
    state.saveData = localStorage.getItem('saveData');
  },
  save(state, saveData) {
    state.saveData = saveData;
    localStorage.setItem('saveData', saveData);
  },
}

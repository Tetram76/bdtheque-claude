// Options of the DocFX modern template for the technical documentation (choix-implementation/documentation-technique.md).
// DocFX passes `mermaid` to mermaid.initialize at each rendering, again when the reader switches between light and dark:
// the getter gives the palette of the theme the page shows.

const font = 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", "Noto Sans", "Liberation Sans", Arial, sans-serif';

// Mermaid derives every other color (actors, notes, states, entities) from these, in its "base" theme only.
const palettes = {
  light: {
    background: '#ffffff',
    primaryColor: '#eaf1fb',
    primaryBorderColor: '#4a6fa5',
    primaryTextColor: '#1f2933',
    secondaryColor: '#fdf3e1',
    tertiaryColor: '#f4f6f8',
    lineColor: '#5f6b7a',
    textColor: '#1f2933',
    edgeLabelBackground: '#ffffff',
    clusterBkg: '#f7f9fc',
    clusterBorder: '#c3cfdd',
    noteBkgColor: '#fff8e1',
    noteBorderColor: '#d9b44a',
  },
  dark: {
    background: '#212529',
    primaryColor: '#24364f',
    primaryBorderColor: '#7aa2d6',
    primaryTextColor: '#e6edf3',
    secondaryColor: '#4a3b22',
    tertiaryColor: '#2b3036',
    lineColor: '#a3adba',
    textColor: '#e6edf3',
    edgeLabelBackground: '#212529',
    clusterBkg: '#262b31',
    clusterBorder: '#4b5563',
    noteBkgColor: '#3d3523',
    noteBorderColor: '#b39545',
  },
};

export default {
  get mermaid() {
    const dark = document.documentElement.dataset.bsTheme === 'dark';
    return {
      theme: 'base',
      darkMode: dark,
      fontFamily: font,
      themeVariables: { ...(dark ? palettes.dark : palettes.light), fontFamily: font },
    };
  },
};

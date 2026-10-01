import type { Config } from "tailwindcss";

const config: Config = {
  content: ["./app/**/*.{ts,tsx}", "./components/**/*.{ts,tsx}", "./lib/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        ink: "#2a3547",
        panel: "#ffffff",
        line: "#e5eaef",
        brand: "#5d87ff",
        accent: "#ffae1f"
      }
    }
  },
  plugins: []
};

export default config;

import type { Config } from "tailwindcss";

const config: Config = {
  content: ["./app/**/*.{ts,tsx}", "./lib/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        ink: "#141414",
        panel: "#f7f7f4",
        line: "#dddddd",
        brand: "#0f766e",
        accent: "#c2410c"
      }
    }
  },
  plugins: []
};

export default config;

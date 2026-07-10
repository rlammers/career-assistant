export default {
  extends: ["@commitlint/config-conventional"],
  plugins: [
    {
      rules: {
        "header-no-emoji": (parsed) => [
          !/\p{Extended_Pictographic}/u.test(parsed.header),
          "commit header must not contain emoji",
        ],
      },
    },
  ],
  rules: {
    "type-enum": [
      2,
      "always",
      ["feat", "fix", "refactor", "test", "docs", "build", "ci", "chore", "security"],
    ],
    "header-no-emoji": [2, "always"],
  },
};

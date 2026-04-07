import { themes as prismThemes } from "prism-react-renderer";
import type { Config } from "@docusaurus/types";
import type * as Preset from "@docusaurus/preset-classic";

const config: Config = {
  title: "SBOM Quality Gate",
  tagline: "Centralized SBOM validation and quality orchestration",
  favicon: "img/favicon.ico",

  // GitHub Pages URL
  url: "https://petercullen68.github.io",
  baseUrl: "/sbom-quality-gate-platform/",

  // GitHub Pages deployment config
  organizationName: "petercullen68",
  projectName: "sbom-quality-gate-platform",
  deploymentBranch: "gh-pages",
  trailingSlash: false,

  onBrokenLinks: "throw",

  i18n: {
    defaultLocale: "en",
    locales: ["en"],
  },

  presets: [
    [
      "classic",
      {
        docs: {
          sidebarPath: "./sidebars.ts",
          editUrl:
            "https://github.com/petercullen68/sbom-quality-gate-platform/tree/main/docs/",
        },
        blog: {
          showReadingTime: true,
          feedOptions: {
            type: ["rss", "atom"],
            xslt: true,
          },
          editUrl:
            "https://github.com/petercullen68/sbom-quality-gate-platform/tree/main/docs/",
        },
        theme: {
          customCss: "./src/css/custom.css",
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    // Social card for link previews
    image: "img/social-card.png",

    navbar: {
      title: "SBOM Quality Gate",
      logo: {
        alt: "SBOM Quality Gate Logo",
        src: "img/logo.svg",
      },
      items: [
        {
          type: "docSidebar",
          sidebarId: "docsSidebar",
          position: "left",
          label: "Docs",
        },
        {
          to: "/docs/api",
          label: "API",
          position: "left",
        },
        { to: "/blog", label: "Blog", position: "left" },
        {
          href: "https://github.com/petercullen68/sbom-quality-gate-platform",
          label: "GitHub",
          position: "right",
        },
      ],
    },

    footer: {
      style: "dark",
      links: [
        {
          title: "Documentation",
          items: [
            {
              label: "Getting Started",
              to: "/docs/getting-started/installation",
            },
            {
              label: "Architecture",
              to: "/docs/architecture/overview",
            },
            {
              label: "API Reference",
              to: "/docs/api",
            },
          ],
        },
        {
          title: "Community",
          items: [
            {
              label: "GitHub Discussions",
              href: "https://github.com/petercullen68/sbom-quality-gate-platform/discussions",
            },
            {
              label: "Issues",
              href: "https://github.com/petercullen68/sbom-quality-gate-platform/issues",
            },
          ],
        },
        {
          title: "More",
          items: [
            {
              label: "Blog",
              to: "/blog",
            },
            {
              label: "GitHub",
              href: "https://github.com/petercullen68/sbom-quality-gate-platform",
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Peter Cullen. Built with Docusaurus.`,
    },

    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ["csharp", "bash", "json", "yaml"],
    },

    // Algolia DocSearch (optional - configure when ready)
    // algolia: {
    //   appId: 'YOUR_APP_ID',
    //   apiKey: 'YOUR_SEARCH_API_KEY',
    //   indexName: 'sbomqualitygate',
    // },
  } satisfies Preset.ThemeConfig,
};

export default config;

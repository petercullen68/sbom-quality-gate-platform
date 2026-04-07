import type { SidebarsConfig } from "@docusaurus/plugin-content-docs";

const sidebars: SidebarsConfig = {
  docsSidebar: [
    "intro",
    {
      type: "category",
      label: "Getting Started",
      collapsed: false,
      items: [
        "getting-started/installation",
        "getting-started/quick-start",
        "getting-started/configuration",
      ],
    },
    {
      type: "category",
      label: "Core Concepts",
      items: [
        "concepts/sbom-overview",
        "concepts/validation-workflow",
        "concepts/quality-scoring",
        "concepts/profiles",
      ],
    },
    {
      type: "category",
      label: "Architecture",
      items: [
        "architecture/overview",
        "architecture/domain-model",
        "architecture/worker-service",
        "architecture/extensibility",
      ],
    },
    {
      type: "category",
      label: "API Reference",
      link: {
        type: "doc",
        id: "api/index",
      },
      items: [
        "api/sboms",
        "api/validation-jobs",
        "api/features",
      ],
    },
    {
      type: "category",
      label: "Integrations",
      items: [
        "integrations/ci-cd",
        "integrations/dependency-track",
      ],
    },
    {
      type: "category",
      label: "Development",
      items: [
        "development/contributing",
        "development/local-setup",
        "development/testing",
      ],
    },
  ],
};

export default sidebars;

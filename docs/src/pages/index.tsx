import clsx from "clsx";
import Link from "@docusaurus/Link";
import useDocusaurusContext from "@docusaurus/useDocusaurusContext";
import Layout from "@theme/Layout";
import Heading from "@theme/Heading";

import styles from "./index.module.css";

function HomepageHeader() {
  const { siteConfig } = useDocusaurusContext();
  return (
    <header className={clsx("hero hero--primary", styles.heroBanner)}>
      <div className="container">
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/getting-started/quick-start"
          >
            Get Started →
          </Link>
          <Link
            className="button button--outline button--lg"
            to="/docs/architecture/overview"
            style={{ marginLeft: "1rem" }}
          >
            Architecture
          </Link>
        </div>
      </div>
    </header>
  );
}

type FeatureItem = {
  title: string;
  description: JSX.Element;
  icon: string;
};

const FeatureList: FeatureItem[] = [
  {
    title: "Single Point of Ingestion",
    icon: "📥",
    description: (
      <>
        Submit your SBOM once and let the platform route it to multiple
        validation tools. No more juggling separate integrations.
      </>
    ),
  },
  {
    title: "Async Validation",
    icon: "⚡",
    description: (
      <>
        Uploads return instantly. Background workers process validation jobs,
        enabling retries and horizontal scaling without blocking your pipeline.
      </>
    ),
  },
  {
    title: "Extensible Architecture",
    icon: "🔌",
    description: (
      <>
        Clean abstractions make it easy to add new validation tools. Implement{" "}
        <code>IValidationTool</code> and plug in sbomqs, Grype, or your own.
      </>
    ),
  },
  {
    title: "Quality Scoring",
    icon: "📊",
    description: (
      <>
        Integrates with sbomqs for comprehensive SBOM quality assessment.
        Configurable pass/fail thresholds per team or product.
      </>
    ),
  },
  {
    title: "Schema Validation",
    icon: "✅",
    description: (
      <>
        Validates SBOMs against CycloneDX 1.4/1.5/1.6 and SPDX 2.3 schemas.
        Catch format errors before they propagate downstream.
      </>
    ),
  },
  {
    title: "CI/CD Ready",
    icon: "🚀",
    description: (
      <>
        RESTful API designed for pipeline integration. Submit SBOMs from GitHub
        Actions, GitLab CI, Jenkins, or any CI system.
      </>
    ),
  },
];

function Feature({ title, icon, description }: FeatureItem) {
  return (
    <div className={clsx("col col--4")}>
      <div className="text--center padding-horiz--md" style={{ marginBottom: "2rem" }}>
        <div style={{ fontSize: "3rem", marginBottom: "1rem" }}>{icon}</div>
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

function HomepageFeatures(): JSX.Element {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}

function HomepageQuickLinks(): JSX.Element {
  return (
    <section className={styles.quickLinks}>
      <div className="container">
        <div className="row">
          <div className="col col--4">
            <div className={styles.quickLinkCard}>
              <h3>📖 Documentation</h3>
              <p>
                Learn how to install, configure, and use SBOM Quality Gate.
              </p>
              <Link to="/docs">Read the docs →</Link>
            </div>
          </div>
          <div className="col col--4">
            <div className={styles.quickLinkCard}>
              <h3>🔧 API Reference</h3>
              <p>
                Complete API documentation with examples and response schemas.
              </p>
              <Link to="/docs/api">Explore API →</Link>
            </div>
          </div>
          <div className="col col--4">
            <div className={styles.quickLinkCard}>
              <h3>💻 GitHub</h3>
              <p>
                View the source, report issues, or contribute to the project.
              </p>
              <Link to="https://github.com/petercullen68/sbom-quality-gate-platform">
                View on GitHub →
              </Link>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

export default function Home(): JSX.Element {
  const { siteConfig } = useDocusaurusContext();
  return (
    <Layout
      title={siteConfig.title}
      description="Centralized SBOM validation and quality orchestration platform"
    >
      <HomepageHeader />
      <main>
        <HomepageFeatures />
        <HomepageQuickLinks />
      </main>
    </Layout>
  );
}

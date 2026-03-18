import logging
from typing import Any, List

from crewai import Agent, Crew, LLM, Process, Task
from crewai.project import CrewBase, agent, before_kickoff, crew, task
from crewai.agents.agent_builder.base_agent import BaseAgent
from army.settings import OLLAMA_API_KEY, OLLAMA_URL

logger = logging.getLogger(__name__)


def _create_llm(model: str) -> LLM:
    return LLM(
        model=model,
        base_url=OLLAMA_URL,
        api_key=OLLAMA_API_KEY,
        headers={"Authorization": f"Bearer {OLLAMA_API_KEY}"},
    )

# If you want to run a snippet of code before or after the crew starts,
# you can use the @before_kickoff and @after_kickoff decorators
# https://docs.crewai.com/concepts/crews#example-crew-class-with-decorators

@CrewBase
class Army():
    """Army crew"""

    agents: List[BaseAgent]
    tasks: List[Task]

    @before_kickoff
    def log_inputs(self, inputs: dict[str, Any]) -> dict[str, Any]:
        newest_important_articles = str(inputs.get("newest_important_articles", "[]"))
        logger.info(
            "Crew kickoff inputs: topic=%s current_year=%s last_read_at_utc=%s newest_important_articles_chars=%s newest_important_articles_preview=%s",
            inputs.get("topic", ""),
            inputs.get("current_year", ""),
            inputs.get("last_read_at_utc", ""),
            len(newest_important_articles),
            newest_important_articles[:1000],
        )
        return inputs

    # Learn more about YAML configuration files here:
    # Agents: https://docs.crewai.com/concepts/agents#yaml-configuration-recommended
    # Tasks: https://docs.crewai.com/concepts/tasks#yaml-configuration-recommended
    
    # If you would like to add tools to your agents, you can learn more about it here:
    # https://docs.crewai.com/concepts/agents#agent-tools
    @agent
    def articles_reader(self) -> Agent:
        return Agent(
            config=self.agents_config['articles_reader'], # type: ignore[index]
            verbose=True,
            llm=_create_llm("ollama_chat/gpt-oss:120b-cloud")
        )

    @agent
    def instructor(self) -> Agent:
        return Agent(
            config=self.agents_config['instructor'], # type: ignore[index]
            verbose=True,
            llm=_create_llm("ollama_chat/gpt-oss:120b-cloud")
        )

    @agent
    def trader(self) -> Agent:
        return Agent(
            config=self.agents_config['trader'], # type: ignore[index]
            verbose=False,
            llm=_create_llm("ollama_chat/glm-5:cloud")
        )

    @agent
    def decision_maker(self) -> Agent:
        return Agent(
            config=self.agents_config['decision_maker'], # type: ignore[index]
            verbose=True,
            llm=_create_llm("ollama_chat/gpt-oss:120b-cloud")
        )

    # To learn more about structured task outputs,
    # task dependencies, and task callbacks, check out the documentation:
    # https://docs.crewai.com/concepts/tasks#overview-of-a-task
    @task
    def research_task(self) -> Task:
        return Task(
            config=self.tasks_config['research_task'], # type: ignore[index]
        )

    @task
    def trading_task(self) -> Task:
        return Task(
            config=self.tasks_config['trading_task'], # type: ignore[index]
            context=[self.research_task()]
        )

    @task
    def decision_task(self) -> Task:
        return Task(
            config=self.tasks_config['decision_task'], # type: ignore[index]
            context=[self.research_task(), self.trading_task()]
        )

    @task
    def reporting_task(self) -> Task:
        return Task(
            config=self.tasks_config['reporting_task'], # type: ignore[index]
            context=[self.decision_task()]
        )

    @crew
    def crew(self) -> Crew:
        """Creates the Army crew"""
        # To learn how to add knowledge sources to your crew, check out the documentation:
        # https://docs.crewai.com/concepts/knowledge#what-is-knowledge

        return Crew(
            agents=self.agents, # Automatically created by the @agent decorator
            tasks=self.tasks, # Automatically created by the @task decorator
            process=Process.sequential,
            verbose=True,
            tracing=True,
            # process=Process.hierarchical, # In case you wanna use that instead https://docs.crewai.com/how-to/Hierarchical/
        )

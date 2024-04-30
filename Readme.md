# Simple Card Game

This is a simple card game inspired by games like Slay the Spire. The game is designed to be played on mobile devices in portrait orientation.

## Classes Used

### Card

- **Description**: Represents individual cards in the game.
- **Attributes**:
  - `Name`: Name of the card.
  - `Description`: Description of the card's effect.
  - `Cost`: Cost of playing the card.
  - `Effect`: Effect of the card, such as attack or defense.

### CardEffect

- **Description**: Represents the effect of a card.
- **Attributes**:
  - `Type`: Type of the effect, either Attack or Defense.
- **Methods**:
  - `ApplyEffect(Player player, Enemy enemy)`: Applies the effect of the card to the player or enemy.

### Player

- **Description**: Represents the player in the game.
- **Attributes**:
  - `Health`: Current health of the player.
  - `MaxHealth`: Maximum health of the player.
  - `Deck`: List of cards in the player's deck.
  - `Hand`: List of cards in the player's hand.
  - `DiscardPile`: List of discarded cards.
- **Methods**:
  - `DrawCard()`: Draws a card from the deck.
  - `DiscardCard(Card card)`: Discards a card from the hand.

### GameManager

- **Description**: Manages the game flow and interactions between player, cards, and encounters.
- **Attributes**:
  - `Player`: Reference to the player object.
- **Methods**:
  - `StartGame()`: Initializes the game.
  - `StartEncounter()`: Starts a new encounter.
  - `ResolvePlayerTurn()`: Handles the player's turn.
  - `ResolveEnemyTurn()`: Handles the enemy's turn.

### Encounter

- **Description**: Represents an encounter with enemies.
- **Attributes**:
  - `Enemies`: List of enemies in the encounter.
- **Methods**:
  - `Start()`: Starts the encounter.
  - `End()`: Ends the encounter.

### Enemy

- **Description**: Represents enemies in the game.
- **Attributes**:
  - `Health`: Current health of the enemy.
- **Methods**:
  - `TakeDamage(int damage)`: Reduces the enemy's health by the specified amount.

## How Classes Work Together

1. The `GameManager` initializes the game by creating a `Player` object and starting the first encounter.

2. During an encounter, the `GameManager` manages turns by resolving actions for the player and enemies.

3. The `Player` draws cards from their deck, plays cards from their hand, and discards cards as needed.

4. Each card's effect, represented by the `CardEffect` class, is applied to the player or enemy as appropriate.

5. The `Encounter` ends when all enemies are defeated or the player's health reaches zero.

6. The `GameManager` handles transitioning between encounters and ending the game.

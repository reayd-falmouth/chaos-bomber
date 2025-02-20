SRC_DIR := src/
GAME_DIR := $(SRC_DIR)/chaos_bomber
ITCH_USER := reayd-falmouth
ITCH_GAME := chaos-bomber
BUILD_DIR=$(GAME_DIR)/build


# Check-in code after formatting
checkin: ## Perform a check-in after formatting the code
    ifndef COMMIT_MESSAGE
		$(eval COMMIT_MESSAGE := $(shell bash -c 'read -e -p "Commit message: " var; echo $$var'))
    endif
	@git add --all; \
	  git commit -m "$(COMMIT_MESSAGE)"; \
	  git push

love:
	@echo "Running love2d game..."
	@cd src; love .